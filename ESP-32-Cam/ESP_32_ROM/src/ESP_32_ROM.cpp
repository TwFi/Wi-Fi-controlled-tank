#include "soc/soc.h" //esp brownout
#include "soc/rtc_cntl_reg.h" //esp brownout
#include <esp_task_wdt.h>
#include "config.h"
#include "types.h"
#include "camera_pins.h"
#include "wifi_and_camera_controller.h"
#include <ArduinoJson.h>
#include <HTTPClient.h>
#include "Wire.h"
#include "SerialTransfer.h"
/* solving conflict beetween ESPAsyncWebServer and esp_http_server*/
#define WEBSERVER_H
#define HTTP_ANY  0b01111111
#include "ESPAsyncWebServer.h"

//pins
constexpr uint8_t rx_pin = 14;
constexpr uint8_t tx_pin = 15;
constexpr uint8_t arduino_reset_pin = 2;

//definitions
void SensorDataInit();
void JsonDataDocInit();
void RecieveDataFromNano();
void TransferDataToNano(DataType data_type);
void HTTP_DataServerInit();
void BuzzerTone(BuzzerSignal sig);
void CheckInitialization();
void EnableWDT();
void DisableWDT();
void Reset_WDT();
void WS_And_SSE_Init();
void WS_Event(AsyncWebSocket * server, AsyncWebSocketClient * client, AwsEventType type, void * arg, uint8_t *data, size_t len);
void SendToWSClient(String type, String message);
void ArduinoWatchdogInit();
void ArduinoWatchdog();
void SendToWSLogFromESP(String message);
void SendToWSLogFromNANO(LogMessage message);
void SendFastSensorData(String data);
void ComboLog(String message);
void SSE_Connect(AsyncEventSourceClient *client);
void SendNanoSettings(NanoSettings set);
void ESPInit();

//const
constexpr uint16_t json_data_size = 1000;
constexpr uint8_t WDT_timeout = 4;//sec
constexpr uint16_t reset_WDT_time = 1500;
constexpr uint16_t arduino_watchdog_time = 2000;
const char *http_param_control = "c";
const char *http_param_beep = "b";
const char *http_param_cam_frame_set = "cam-r";
const char *http_param_cam_quality_set = "cam-q";
const char *http_param_cam_gain_set = "cam-g";
const char *http_param_nano_set_protection = "n-p";
const char *http_response_str = "OK";
const char *http_response_mime_type = "text/plain";
const char *ws_log_type_esp = "logE";
const char *ws_log_type_nano = "logN";
const char *sse_event_fast_sensor = "fast";

//json names
const char *jsn_name_dht_huminidy 		= "hum";
const char *jsn_name_dht_temperature 	= "tmp";
const char *jsn_name_us_distance 		= "u_dst";
const char *jsn_name_mpu_acc 			= "acc";
const char *jsn_name_mpu_gyro 			= "gyr";
const char *jsn_name_mpu_angle 			= "ang";
const char *jsn_name_bmp_pressure 		= "prs";
const char *jsn_name_bmp_altitude 		= "alt";
const char *jsn_name_bat_voltage 		= "b_vlt";
const char *jsn_name_bat_percente 		= "bpc";
const char *jsn_name_rps 				= "rps";
const char *jsn_name_speed 				= "spd";
const char *jsn_name_mileage 			= "mil";
const char *jsn_name_wifi_signal 		= "sig";
const char *jsn_name_rpt_bat_vol		= "r_bat";
const char *jsn_name_rpt_bat_perc		= "r_bat_p";

//vars
uint32_t timer_send_GET_data = 0;
uint32_t timer_WDT;
SensorData s_data;
FastSensorData fs_data;
ESP_SensorData self_s_data;
NanoSettings nano_set;
NanoSettings current_nano_set;
StaticJsonDocument<json_data_size> json_data_doc;
SerialTransfer uart_transfer;
MoveSide move_side;
BuzzerSignal buzzer_signal;
AsyncWebServer http_data_server(DATA_HTTP_SERVER_PORT);
AsyncWebSocket wsocket(F("/ws"));
AsyncEventSource sse(F("/sse"));
AsyncWebSocketClient *ws_client;
uint32_t timer_arduino_watchdog = 0;
bool arduino_rebooted = false;
bool init_is_good = true;
bool WDT_is_on = false;

void setup() 
{
	//log_d("Free heap: %d", ESP.getFreeHeap());
	//log_d("Free PSRAM: %d", ESP.getFreePsram());
	ESPInit();
	WiFiInit();
	CameraInit();
	SensorDataInit();
	JsonDataDocInit();
	HTTP_DataServerInit();

	CheckInitialization();
	EnableWDT();
	ArduinoWatchdogInit();
}

void loop() 
{
	Reset_WDT();
	WiFiUpdate();
	RecieveDataFromNano();
	ArduinoWatchdog();
}

void ESPInit()
{
	WRITE_PERI_REG(RTC_CNTL_BROWN_OUT_REG, 0); //disable brownout detector
	esp_task_wdt_init(WDT_timeout, true);
    Serial.begin(SERIAL_SPEED);
	Serial1.begin(SERIAL_1_SPEED, SERIAL_8N1, rx_pin, tx_pin);
	uart_transfer.begin(Serial1, false);	
}

void ArduinoWatchdogInit()
{
	pinMode(arduino_reset_pin, OUTPUT);
	digitalWrite(arduino_reset_pin, HIGH);
	timer_arduino_watchdog =  millis() + arduino_watchdog_time * 2;
}

void ArduinoWatchdog()
{
	if (millis() > timer_arduino_watchdog && !arduino_rebooted)
	{
		digitalWrite(arduino_reset_pin, LOW);
		delay(1);
		digitalWrite(arduino_reset_pin, HIGH);
		ComboLog(F("NANO has been rebooted!"));
		timer_arduino_watchdog = millis() + arduino_watchdog_time;
		arduino_rebooted = true;
	}
}

void EnableWDT()
{
	WDT_is_on = true;
  	esp_task_wdt_add(NULL);
	timer_WDT = millis() + reset_WDT_time;
}
void DisableWDT()
{
	WDT_is_on = false;
	esp_task_wdt_delete(NULL);
}

void Reset_WDT()
{
	if(WDT_is_on)
	{
		if (millis() > timer_WDT)
		{
			esp_task_wdt_reset();
			timer_WDT = millis() + reset_WDT_time;
		}
	}
	else
	{
		EnableWDT();
	}
}

void CheckInitialization()
{
	if (init_is_good)
	{
		BuzzerTone(BuzzerSignal::GOOD);
	}
	else
	{
		BuzzerTone(BuzzerSignal::ERROR);
	}
}

void BuzzerTone(BuzzerSignal sig)
{
	buzzer_signal = sig;
	TransferDataToNano(DataType::BUZZER_DATA);
}

void TransferDataToNano(DataType data_type)
{
	uint16_t send_size = 0;
	send_size = uart_transfer.txObj(data_type, send_size);

	switch (data_type)
	{
		case DataType::CONTROL_DATA:
			send_size = uart_transfer.txObj(move_side, send_size);
			break;
		case DataType::BUZZER_DATA:
			send_size = uart_transfer.txObj(buzzer_signal, send_size);
			break;
		case DataType::NANO_SETTINGS:
			send_size = uart_transfer.txObj(nano_set, send_size);
			break;
		case DataType::GET_NANO_SETTINGS:
			break;
		default:
			uart_transfer.reset();
			return;
	}

	uart_transfer.sendData(send_size);	
}

void SensorDataInit()
{
	s_data = {0,0,0,{0,0,0},{0,0,0},{0,0,0},0,0,0,0,0,0,0};
	self_s_data = {0,0};
}

void JsonDataDocInit()
{
	//data from nano
	json_data_doc[jsn_name_dht_huminidy] = 0;
	json_data_doc[jsn_name_dht_temperature] = 0;
	json_data_doc[jsn_name_us_distance] = 0;
	
	json_data_doc[jsn_name_mpu_acc][0] = 0;
	json_data_doc[jsn_name_mpu_acc][1] = 0;
	json_data_doc[jsn_name_mpu_acc][2] = 0;
	
	json_data_doc[jsn_name_mpu_gyro][0] = 0;
	json_data_doc[jsn_name_mpu_gyro][1] = 0;
	json_data_doc[jsn_name_mpu_gyro][2] = 0;
	
	json_data_doc[jsn_name_mpu_angle][0] = 0;
	json_data_doc[jsn_name_mpu_angle][1] = 0;
	json_data_doc[jsn_name_mpu_angle][2] = 0;
	
	json_data_doc[jsn_name_bmp_pressure] = 0;
	json_data_doc[jsn_name_bmp_altitude] = 0;
	json_data_doc[jsn_name_bat_voltage] = 0;
	json_data_doc[jsn_name_bat_percente] = 0;
	json_data_doc[jsn_name_rps] = 0;
	json_data_doc[jsn_name_speed] = 0;
	json_data_doc[jsn_name_mileage] = 0;

	//additional data
	json_data_doc[jsn_name_wifi_signal] = 0;
	json_data_doc[jsn_name_rpt_bat_vol] = 0;
	json_data_doc[jsn_name_rpt_bat_perc] = 0;
}

void SensorDataToJsonDoc()
{
	//data from nano
	json_data_doc[jsn_name_dht_huminidy] = s_data.dht_huminidy;
	json_data_doc[jsn_name_dht_temperature] = s_data.dht_temperature;
	json_data_doc[jsn_name_us_distance] =  s_data.us_distance;
	
	json_data_doc[jsn_name_mpu_acc][0] = s_data.mpu_acc[0];
	json_data_doc[jsn_name_mpu_acc][1] = s_data.mpu_acc[1];
	json_data_doc[jsn_name_mpu_acc][2] = s_data.mpu_acc[2];
	
	json_data_doc[jsn_name_mpu_gyro][0] = s_data.mpu_gyro[0];
	json_data_doc[jsn_name_mpu_gyro][1] = s_data.mpu_gyro[1];
	json_data_doc[jsn_name_mpu_gyro][2] = s_data.mpu_gyro[2];
	
	json_data_doc[jsn_name_mpu_angle][0] = s_data.mpu_angle[0];
	json_data_doc[jsn_name_mpu_angle][1] = s_data.mpu_angle[1];
	json_data_doc[jsn_name_mpu_angle][2] = s_data.mpu_angle[2];
	
	json_data_doc[jsn_name_bmp_pressure] = s_data.bmp_pressure;
	json_data_doc[jsn_name_bmp_altitude] = s_data.bmp_altitude;
	json_data_doc[jsn_name_bat_voltage] = s_data.bat_voltage;
	json_data_doc[jsn_name_bat_percente] = s_data.bat_percente;
	json_data_doc[jsn_name_rps] = s_data.rps;
	json_data_doc[jsn_name_speed] = s_data.speed;
	json_data_doc[jsn_name_mileage] = s_data.mileage;

	//additional data
	json_data_doc[jsn_name_wifi_signal] = WiFi.RSSI();
	json_data_doc[jsn_name_rpt_bat_vol] = self_s_data.rpt_bat_vol;
	json_data_doc[jsn_name_rpt_bat_perc] = self_s_data.rpt_bat_perc;
}

void RecieveDataFromNano()
{
	if(uart_transfer.available())
	{
		uint16_t rec_size = 0;
		DataType data_type;
		rec_size = uart_transfer.rxObj(data_type, rec_size);

		switch (data_type)
		{
			case DataType::SENSOR_DATA:
				uart_transfer.rxObj(s_data, rec_size);
				break;
			case DataType::WATCHDOG_DATA:
				if (arduino_rebooted)
				{
					BuzzerTone(BuzzerSignal::WARNING);
					arduino_rebooted = false;
				}
				timer_arduino_watchdog = millis() + arduino_watchdog_time;
				break;
			case DataType::LOG_DATA:
			{
				LogMessage log;
				uart_transfer.rxObj(log, rec_size);
				SendToWSLogFromNANO(log);
				break;
			}
			case DataType::FAST_SENSOR_DATA:
			{
				uart_transfer.rxObj(fs_data, rec_size);
				String fast_sensor_data_json = String(F("{\"ultrasonic\":")) + String(fs_data.ultrasonic) + F(",\"x\":") + String(fs_data.x) + F(",\"y\":")
				+ String(fs_data.y) + F(",\"z\":") + String(fs_data.z) + F("}");
				SendFastSensorData(fast_sensor_data_json);
				break;
			}
			case DataType::NANO_SETTINGS:
			{
				uart_transfer.rxObj(current_nano_set, rec_size);
				SendToWSClient(F("nset"), String(F("{\"protectionOverload\":")) + String((int)current_nano_set.protection_overload) + F("}"));
				break;
			}
			default:
				//Serial.println(F("ESP Unknown received data type!"));
				break;
		}
	}
}

void HTTP_DataServerInit()
{
 	http_data_server.on("/data", HTTP_GET, [](AsyncWebServerRequest *request)
	{
		String json_string;
		SensorDataToJsonDoc();
		serializeJson(json_data_doc, json_string);
		request->send(200, http_response_mime_type, json_string);
  	});

	http_data_server.on("/", HTTP_GET, [](AsyncWebServerRequest *request)
	{
		if(request->hasParam(http_param_control))
		{
			String value = request->getParam(http_param_control)->value();
			move_side = (MoveSide)(value.toInt());
			TransferDataToNano(DataType::CONTROL_DATA);
			
			request->send(200, http_response_mime_type, http_response_str);
		}
		else if (request->hasParam(http_param_beep))
		{
			String value = request->getParam(http_param_beep)->value();
			BuzzerTone((BuzzerSignal)(value.toInt()));
			
			request->send(200, http_response_mime_type, http_response_str);
		}
		else if (request->hasParam(http_param_cam_frame_set))
		{
			int framesize = request->getParam(http_param_cam_frame_set)->value().toInt();

			SetCameraFramesize(framesize);

			if (request->hasParam(http_param_cam_quality_set))
			{
				int quality = request->getParam(http_param_cam_quality_set)->value().toInt();
				SetCameraQuality(quality);
			}
			if (request->hasParam(http_param_cam_gain_set))
			{
				int gain = request->getParam(http_param_cam_gain_set)->value().toInt();
				SetCameraGain(gain);
			}
			request->send(200, http_response_mime_type, http_response_str);
		}
		else if (request->hasParam(http_param_nano_set_protection))
		{
			nano_set.protection_overload = (bool)request->getParam(http_param_nano_set_protection)->value().toInt();
			TransferDataToNano(DataType::NANO_SETTINGS);
			request->send(200, http_response_mime_type, http_response_str);
		}
	});

	http_data_server.on("/g-cam", HTTP_GET, [](AsyncWebServerRequest *request)
	{
		int frame = -1;
		int quality = -1;
		int gain = -1;
		int *p_frame = &frame;
		int *p_quality = &quality;
		int *p_gain = &gain;

		GetCameraSettings(p_frame, p_quality, p_gain);
		request->send(200, http_response_mime_type, String(F("{\"frame\":")) + String(frame) + F(",\"quality\":") + String(quality)
		 + F(",\"gain\":") + String(gain) + F("}"));
  	});

	http_data_server.on("/g-n", HTTP_GET, [](AsyncWebServerRequest *request)
	{
		TransferDataToNano(DataType::GET_NANO_SETTINGS);
		request->send(200, http_response_mime_type, http_response_str);
  	});

	http_data_server.on("/repeater-bat", HTTP_GET, [](AsyncWebServerRequest *request)
	{
		if (request->hasParam(F("bat_v")) && request->hasParam(F("bat_p")))
		{
			self_s_data.rpt_bat_vol = request->getParam(F("bat_v"))->value().toFloat();
			self_s_data.rpt_bat_perc = request->getParam(F("bat_p"))->value().toInt();
		}
  	});

	http_data_server.on("/ping", HTTP_GET, [](AsyncWebServerRequest *request)
	{
		request->send(200, http_response_mime_type, F("pong"));
  	});

	WS_And_SSE_Init();
	http_data_server.begin();
}

void WS_And_SSE_Init()
{
	wsocket.onEvent(WS_Event);
	sse.onConnect(SSE_Connect);
	http_data_server.addHandler(&sse);
	http_data_server.addHandler(&wsocket);
}

void SSE_Connect(AsyncEventSourceClient *client)
{
	SendToWSLogFromESP(F("SSE connection established!"));
}

void WS_Event(AsyncWebSocket * server, AsyncWebSocketClient * client, AwsEventType e_type, void * arg, uint8_t *data, size_t len)
{
	switch (e_type)
	{
		case WS_EVT_CONNECT:
		{
			ws_client = client;
			break;
		}
		case WS_EVT_DISCONNECT:
		{
			ws_client = nullptr;
			break;
		}
		case WS_EVT_ERROR:
		{
			break;
		}

		case WS_EVT_DATA:
		{
			AwsFrameInfo * info = (AwsFrameInfo*)arg;
			if(info->final && info->index == 0 && info->len == len)
			{
				if(info->opcode == WS_TEXT)
				{
					data[len] = 0;
					String message  = String((char*)data);
					String type_message = message.substring(0,4);//first 4 characters it is type message
					
					if (type_message == F("ping"))
					{
						client->text(F("pong"));
					}
					else if (type_message == F("hi__"))
					{
						SendToWSLogFromESP(F("WS connection established!"));
					}
				} 
			}
			break;
		}
		default:
			break;
	}
}

void SendToWSClient(String type, String message)
{
	if (ws_client != nullptr)
	{
		ws_client->text(type + message);
	}
}

void SendToWSLogFromESP(String message)
{
	SendToWSClient(ws_log_type_esp, message);
}

void SendToWSLogFromNANO(LogMessage message)
{
	if (message == LogMessage::MOTOR_OVERLOAD)
	{
		SendToWSClient(ws_log_type_nano, F("Motor protection detected!"));
	}
}

void SendFastSensorData(String data)
{
	sse.send(data.c_str(), sse_event_fast_sensor, millis());
}

void ComboLog(String message)
{
	Serial.println(message);
	SendToWSLogFromESP(message);
}

void SendNanoSettings(NanoSettings set)
{
	nano_set = set;
	TransferDataToNano(DataType::NANO_SETTINGS);
}