#include <Arduino.h>
#include <SimpleDHT.h>
#include "NewPing.h"
#include "Wire.h"
#include "MPU6050_light.h"
#include "PinChangeInterrupt.h"
#include "SerialTransfer.h"
#include "NewTone.h"
#include "Adafruit_BMP280.h"

//__attribute__((__packed__)) for correct transmission through the serial transfer 
enum __attribute__((__packed__)) MoveSide
{
	FORWARD,//0
	BACK,	//1
	LEFT,	//2
	RIGHT,	//3
	STOP,	//4
	FORWARD_LEFT, //5
	FORWARD_RIGHT, //6
	BACK_LEFT, //7
	BACK_RIGHT //8
};

enum EngineType
{
	LEFT_ENG,
	RIGHT_ENG
};

enum __attribute__((__packed__)) DataType
{
	SENSOR_DATA,
	CONTROL_DATA,
	BUZZER_DATA,
	WATCHDOG_DATA,
	LOG_DATA,
	FAST_SENSOR_DATA,
	NANO_SETTINGS,
	GET_NANO_SETTINGS
};

enum __attribute__((__packed__)) BuzzerSignal
{
	NOTIFICATION,
	ERROR,
	GOOD,
	WARNING
};

struct __attribute__((__packed__)) SensorData
{
	uint8_t dht_huminidy;
	float dht_temperature;
	uint16_t us_distance;
	float mpu_acc[3];
	float mpu_gyro[3];
	float mpu_angle[3];
	float bmp_pressure;
	float bmp_altitude;
	float bat_voltage;
	uint8_t bat_percente;
	uint8_t rps;
	float speed; // cm/s
	float mileage;// cm
};

struct  __attribute__((__packed__)) FastSensorData
{
	uint16_t ultrasonic;
	float x;
	float y;
	float z;
};

enum __attribute__((__packed__)) LogMessage
{
	MOTOR_OVERLOAD
};

struct __attribute__((__packed__)) NanoSettings
{
	bool protection_overload;
};

//Definitions
void IntegralLEDInit();
void EnginesInit();
void ReceiveData();
void SendData (DataType type);
void ReadBattery();
void Interrupt_A3144();
void Read_A3144();
void Blink();
void Blink(int count, int _delay);
void EnginesInit();
void Move(MoveSide side);
void EngineMove(EngineType eng, MoveSide side, uint8_t pwm);
void EngineMove(EngineType eng, MoveSide side);
void SensorsInit();
void SensorsUpdateAndSend();
void OverloadProtection();
float RoundFloatValueToTwoDigit(float value);
void BuzzerTone(BuzzerSignal signal);
void BuzzerUpdate();
void AutoStopMove();
void HeartbeatLED();
void SendWatchdogDataAndHearbeat();
void InitSettings();

//const
constexpr uint32_t serial_speed  					= 76800;
constexpr uint32_t i2c_speed  						= 100000;
constexpr uint16_t sensor_update_time 				= 2000;
constexpr uint16_t fast_sensor_update_time			= 400;
constexpr uint8_t max_US025_distance 				= 150;
constexpr float sea_level 							= 1013.25;
constexpr float voltage_supply 						= 4.26;
constexpr float battery_min_voltage 				= 3.1;
constexpr float battery_max_voltage 				= 4.2;
constexpr uint8_t A3144_timeout						= 10;//time between read
constexpr float wheel_diameter 						= 1.7;//cm
constexpr float voltage_overload_protection_limit 	= 3.25;
constexpr uint8_t overload_protection_update_time 	= 5; 
constexpr uint8_t overload_protection_low_v_time	= 10;//time until protection
constexpr uint8_t engine_pwm_max_left 				= 245; //0-255
constexpr uint8_t engine_pwm_max_right 				= 255; //0-255
constexpr float pwm_turn_mult_left 					= 0.45f;//0-1
constexpr float pwm_turn_mult_right 				= 0.7f;//0-1
constexpr uint16_t auto_stop_move_time				= 1000; //ms
constexpr float bat_voltage_range 					= battery_max_voltage - battery_min_voltage;
constexpr uint16_t buzzer_frequency[4][3] 			= {{1000, 300, 1000}, //Notification
													{200, 100, 200}, 	//Error
													{100, 200, 350},	//Good
													{500, 300, 100}};	//Warning
constexpr uint16_t buzzer_time[4][3] 				= {{500, 500, 500}, //Notification
													{1000, 1000, 1000}, //Error
													{300, 150, 300},	//Good
													{300, 100, 50}};	//Warning
constexpr uint16_t send_watchdog_data_time 			= 1000;

//Pins
constexpr uint8_t pin_DHT 							= 2;
constexpr uint8_t pin_US025_trigger 				= 3;
constexpr uint8_t pin_US025_echo 					= 4;
constexpr uint8_t pin_eng_1_1 						= 5; //right eng
constexpr uint8_t pin_eng_1_2 						= 6; //right eng
constexpr uint8_t pin_eng_2_1 						= 7; //left eng
constexpr uint8_t pin_eng_2_2						= 8; //left eng
constexpr uint8_t pin_A3144 						= 9;
constexpr uint8_t pin_ENA 							= 10; //pwm left eng
constexpr uint8_t pin_ENB 							= 11; //pwm right eng
constexpr uint8_t pin_LED 							= 13;
constexpr uint8_t pin_v_bat 						= 14;
constexpr uint8_t pin_buzzer 						= 12;

//vars
SimpleDHT11 	dht(pin_DHT);
NewPing 		sonar(pin_US025_trigger, pin_US025_echo, max_US025_distance);
MPU6050 		mpu(Wire);
Adafruit_BMP280 bmp;
SerialTransfer 	uart_transfer;
SensorData 		s_data;
FastSensorData 	fs_data;
NanoSettings 	settings;
uint32_t timer_sensor_update = 0;
uint32_t timer_fast_sensor_update = 0;
uint32_t timer_a3144_rps = 0;
uint32_t timer_a3144_last_activation = 0;
uint32_t timer_overload_protection_update = 0;
uint32_t timer_overload_protection_low_v = 0;
uint32_t timer_auto_stop_move = 0;
volatile uint16_t turnovers = 0;
MoveSide current_move_side = MoveSide::STOP;
bool overload_protection_last_voltage_is_low = false;
bool buzzer_is_tone = false;
uint32_t timer_buzzer = 0;
uint8_t buzzer_current_signal_number = 0;
BuzzerSignal buzzer_current_signal;
bool integrated_led = false;
uint32_t timer_watchdog = 0; 
LogMessage log_message;

void setup()
{
	InitSettings();
	IntegralLEDInit();
	Blink();

	Serial.begin(serial_speed);
	Wire.begin();
	Wire.setClock(i2c_speed);
	uart_transfer.begin(Serial, false);

	EnginesInit();
	SensorsInit();
}

void loop()
{
	SendWatchdogDataAndHearbeat();
	ReceiveData();
	OverloadProtection();
	SensorsUpdateAndSend();
	BuzzerUpdate();
	AutoStopMove();
}

void IntegralLEDInit()
{
	pinMode(pin_LED, OUTPUT);
}

void ReceiveData()
{
	if (uart_transfer.available())
	{
		uint16_t rec_size = 0;
		DataType data_type;
		rec_size = uart_transfer.rxObj(data_type, rec_size);

		switch (data_type)
		{
			case DataType::CONTROL_DATA:
				MoveSide move_side;
				uart_transfer.rxObj(move_side, rec_size);
				Move(move_side);
				break;
			case DataType::BUZZER_DATA:
				BuzzerSignal bs;
				uart_transfer.rxObj(bs, rec_size);
				BuzzerTone(bs);
				break;
			case DataType::NANO_SETTINGS:
				uart_transfer.rxObj(settings, rec_size);
				break;
			case DataType::GET_NANO_SETTINGS:
				SendData(DataType::NANO_SETTINGS);
				break;
			default:
				//Serial.println(F("NANO Unknown received data type!"));
				break;
		}
	}
}

void AutoStopMove()
{
	if (current_move_side != MoveSide::STOP && millis() >= timer_auto_stop_move)
	{
		Move(MoveSide::STOP);
	}
}

void SensorDataInit()
{
	s_data = {0,0,0,{0,0,0},{0,0,0},{0,0,0},0,0,0,0,0,0,0};
	fs_data = {0,0,0,0};
}


void SendData(DataType type)
{
	uint16_t send_size = 0;
	send_size = uart_transfer.txObj(type, send_size);

	switch (type)
	{
		case DataType::WATCHDOG_DATA:
			break;
		case DataType::SENSOR_DATA:
			send_size = uart_transfer.txObj(s_data, send_size);
			break;
		case DataType::FAST_SENSOR_DATA:
			send_size = uart_transfer.txObj(fs_data, send_size);
			break;
		case DataType::LOG_DATA:
			send_size = uart_transfer.txObj(log_message, send_size);
			break;
		case DataType::NANO_SETTINGS:
			send_size = uart_transfer.txObj(settings, send_size);
			break;
		default:
			uart_transfer.reset();
			return;
	}

	uart_transfer.sendData(send_size);
}

void SendWatchdogDataAndHearbeat()
{
	if (millis() >= timer_watchdog)
	{
		timer_watchdog = millis() + send_watchdog_data_time;

		SendData(DataType::WATCHDOG_DATA);
		HeartbeatLED();
	}
}

void ReadBattery()
{
	s_data.bat_voltage = RoundFloatValueToTwoDigit((float)(analogRead(pin_v_bat) * voltage_supply) / 1024);

	if (s_data.bat_voltage <= battery_min_voltage)
	{
		s_data.bat_percente = 0;
	}
	else if (s_data.bat_voltage >= battery_max_voltage)
	{
		s_data.bat_percente = 100;
	}
	else
	{
		s_data.bat_percente = (float)(s_data.bat_voltage - battery_min_voltage) / bat_voltage_range * 100;
	}
}

void Interrupt_A3144()
{
	if (millis() >= timer_a3144_last_activation + A3144_timeout)
	{
		turnovers++;
	}
}

void Read_A3144()
{
	if (millis() >= timer_a3144_rps)
	{
		timer_a3144_rps = millis() + 1000;
		
		s_data.rps = turnovers;
		s_data.speed = RoundFloatValueToTwoDigit(s_data.rps * wheel_diameter * PI);
		s_data.mileage = s_data.mileage + s_data.speed;
		
		turnovers = 0;
	}
}

void Blink()
{
	Blink(5, 20);
}

void Blink(int count, int _delay)
{
	integrated_led = true;
	for (int i = 0; i < count; i++)
	{
		digitalWrite(pin_LED, integrated_led);
		delay(_delay);
		integrated_led = !integrated_led;
	}
	
	digitalWrite(pin_LED, false);
}

void HeartbeatLED()
{
	integrated_led = !integrated_led;
	digitalWrite(pin_LED, integrated_led);
}

void EnginesInit()
{
	pinMode(pin_eng_1_1, OUTPUT);
	pinMode(pin_eng_1_2, OUTPUT);
	pinMode(pin_eng_2_1, OUTPUT);
	pinMode(pin_eng_2_2, OUTPUT);
	pinMode(pin_ENA, OUTPUT);
	pinMode(pin_ENB, OUTPUT);
	
	digitalWrite(pin_eng_1_1, LOW);
	digitalWrite(pin_eng_1_2, LOW);
	digitalWrite(pin_eng_2_1, LOW);
	digitalWrite(pin_eng_2_2, LOW);
	
	analogWrite(pin_ENA, 0);//LEFT
	analogWrite(pin_ENB, 0);//RIGHT
}

void Move(MoveSide side)
{
	timer_auto_stop_move = millis() + auto_stop_move_time;

	if (side == current_move_side && side != MoveSide::STOP)
	{
		return;
	}

	if (buzzer_is_tone)
	{
		side = MoveSide::STOP;
	}

	//turnovers zeroing
	if (side == MoveSide::STOP || side == MoveSide::LEFT || side == MoveSide::RIGHT)
	{
		turnovers = 0;
	}
	else if (side == MoveSide::FORWARD || side == MoveSide::FORWARD_LEFT || side == MoveSide::FORWARD_RIGHT)
	{
		if (current_move_side == MoveSide::BACK || current_move_side == MoveSide::BACK_LEFT || current_move_side == MoveSide::BACK_RIGHT)
		{
			turnovers = 0;
		}
	}
	else if (side == MoveSide::BACK || side == MoveSide::BACK_LEFT || side == MoveSide::BACK_RIGHT)
	{
		if (current_move_side == MoveSide::FORWARD || current_move_side == MoveSide::FORWARD_LEFT || current_move_side == MoveSide::FORWARD_RIGHT)
		{
			turnovers = 0;
		}
	}

	switch(side)
	{
		case MoveSide::FORWARD:
			EngineMove(EngineType::LEFT_ENG, MoveSide::FORWARD);
			EngineMove(EngineType::RIGHT_ENG, MoveSide::FORWARD);
			break;
		case MoveSide::BACK:
			EngineMove(EngineType::LEFT_ENG, MoveSide::BACK);
			EngineMove(EngineType::RIGHT_ENG, MoveSide::BACK);
			break;
		case MoveSide::LEFT:
			EngineMove(EngineType::LEFT_ENG, MoveSide::BACK);
			EngineMove(EngineType::RIGHT_ENG, MoveSide::FORWARD);
			break;
		case MoveSide::RIGHT:
			EngineMove(EngineType::LEFT_ENG, MoveSide::FORWARD);
			EngineMove(EngineType::RIGHT_ENG, MoveSide::BACK);
			break;
		case MoveSide::STOP:
			EngineMove(EngineType::LEFT_ENG, MoveSide::STOP);
			EngineMove(EngineType::RIGHT_ENG, MoveSide::STOP);
			break;
		case MoveSide::FORWARD_LEFT:
		{
			uint8_t pwm = (uint8_t)255 * pwm_turn_mult_left;
			EngineMove(EngineType::LEFT_ENG, MoveSide::FORWARD);
			EngineMove(EngineType::RIGHT_ENG, MoveSide::FORWARD, pwm);
			break;
		}
		case MoveSide::FORWARD_RIGHT:
		{
			uint8_t pwm = (uint8_t)255 * pwm_turn_mult_right;
			EngineMove(EngineType::LEFT_ENG, MoveSide::FORWARD, pwm);
			EngineMove(EngineType::RIGHT_ENG, MoveSide::FORWARD);
			break;
		}
		case MoveSide::BACK_LEFT:
		{
			uint8_t pwm = (uint8_t)255 * pwm_turn_mult_left;
			EngineMove(EngineType::LEFT_ENG, MoveSide::BACK);
			EngineMove(EngineType::RIGHT_ENG, MoveSide::BACK, pwm);
			break;
		}
		case MoveSide::BACK_RIGHT:
		{
			uint8_t pwm = (uint8_t)255 * pwm_turn_mult_right;
			EngineMove(EngineType::LEFT_ENG, MoveSide::BACK, pwm);
			EngineMove(EngineType::RIGHT_ENG, MoveSide::BACK);
			break;
		}
		default:
			//Serial.println(F("ERORR: Unknow move side"));
			break;
	}
	current_move_side = side;
}

void EngineMove(EngineType eng, MoveSide side, uint8_t pwm)
{
	uint8_t pin_1 = -1;
	uint8_t pin_2 = -1;
	
	if(eng == EngineType::RIGHT_ENG)
	{
		pin_1 = pin_eng_1_1;
		pin_2 = pin_eng_1_2;
		analogWrite(pin_ENB, pwm);
	}
	else if(eng == EngineType::LEFT_ENG)
	{
		pin_1 = pin_eng_2_1;
		pin_2 = pin_eng_2_2;
		analogWrite(pin_ENA, pwm);
	}
	else
	{
		//Serial.println(F("ERORR: Unknow engine type"));
		return;
	}
	
	switch(side)
	{
		case MoveSide::FORWARD:
			digitalWrite(pin_2, LOW);
			digitalWrite(pin_1, HIGH);
			break;
		case MoveSide::BACK:
			digitalWrite(pin_1, LOW);
			digitalWrite(pin_2, HIGH);
			break;
		case MoveSide::STOP:
			digitalWrite(pin_1, LOW);
			digitalWrite(pin_2, LOW);

			break;
		default:
			//Serial.println(F("ERORR: Unknow move side"));
			break;
	}
}

void EngineMove(EngineType eng, MoveSide side)
{
	if (eng == EngineType::LEFT_ENG)
	{
		EngineMove(eng, side, engine_pwm_max_left);
	}
	else
	{
		EngineMove(eng, side, engine_pwm_max_right);
	}
}

void SensorsInit()
{	
	//MPU
	mpu.begin();
	mpu.calcOffsets(true, true);

	//BMP
	bmp.begin(BMP280_ADDRESS_ALT, BMP280_CHIPID);
	bmp.setSampling(Adafruit_BMP280::MODE_NORMAL,     /* Operating Mode. */
                  Adafruit_BMP280::SAMPLING_X2,     /* Temp. oversampling */
                  Adafruit_BMP280::SAMPLING_X8,    /* Pressure oversampling */
                  Adafruit_BMP280::FILTER_X8,      /* Filtering. */
                  Adafruit_BMP280::STANDBY_MS_500); /* Standby time. */

	//A3144	
	attachPCINT(digitalPinToPCINT(pin_A3144), Interrupt_A3144, FALLING);
	timer_a3144_rps = millis() + 1000;
	timer_a3144_last_activation = 0;
	
	timer_sensor_update = millis() + sensor_update_time;
	timer_fast_sensor_update = millis() + fast_sensor_update_time;
}

void SensorsUpdateAndSend()
{
	//Hall sensor, speed, rps, milliage
	Read_A3144();

	//NORMAL SENSORS
	if (millis() >= timer_sensor_update)
	{
		timer_sensor_update = millis() + sensor_update_time;
		
		//BATTERY
		ReadBattery();
	
		//SONAR
		s_data.us_distance = sonar.ping_cm();
		
		//MPU6050
		mpu.update();
		s_data.mpu_acc[0] = RoundFloatValueToTwoDigit(mpu.getAccX());
		s_data.mpu_acc[1] = RoundFloatValueToTwoDigit(mpu.getAccY());
		s_data.mpu_acc[2] = RoundFloatValueToTwoDigit(mpu.getAccZ());
		s_data.mpu_gyro[0] = RoundFloatValueToTwoDigit(mpu.getGyroX());
		s_data.mpu_gyro[1] = RoundFloatValueToTwoDigit(mpu.getGyroY());
		s_data.mpu_gyro[2] = RoundFloatValueToTwoDigit(mpu.getGyroZ());
		s_data.mpu_angle[0] = RoundFloatValueToTwoDigit(mpu.getAngleX());
		s_data.mpu_angle[1] = RoundFloatValueToTwoDigit(mpu.getAngleY());
		s_data.mpu_angle[2] = RoundFloatValueToTwoDigit(mpu.getAngleZ());
		
		//BMP280
		s_data.bmp_pressure = bmp.readPressure();
		s_data.bmp_altitude = bmp.readAltitude(sea_level);

		//DHT
		float temperature, humidity;
		int error_code = dht.read2(&temperature, &humidity, NULL);
		if (error_code == SimpleDHTErrSuccess)
		{
			s_data.dht_temperature = temperature;
			s_data.dht_huminidy = (uint8_t)humidity;
		}

		SendData(DataType::SENSOR_DATA);
	}

	//FAST SENSORS
	if (millis() >= timer_fast_sensor_update)
	{
		timer_fast_sensor_update = millis() + fast_sensor_update_time;
		
		fs_data.ultrasonic = sonar.ping_cm();

		mpu.update();
		fs_data.x = RoundFloatValueToTwoDigit(mpu.getAngleX());
		fs_data.y = RoundFloatValueToTwoDigit(mpu.getAngleY());
		fs_data.z = RoundFloatValueToTwoDigit(mpu.getAngleZ());

		SendData(DataType::FAST_SENSOR_DATA);
	}
}

void OverloadProtection()
{
	if (current_move_side != MoveSide::STOP && millis() >= timer_overload_protection_update && settings.protection_overload)
	{
		timer_overload_protection_update = millis() + overload_protection_update_time;
			
		float battery_v = (float)(analogRead(pin_v_bat) * voltage_supply) / 1024;
		
		//if battery voltage is low
		if (battery_v <= voltage_overload_protection_limit)//0.004 adc resolution 
		{
			if (overload_protection_last_voltage_is_low)
			{
				if(millis() >= timer_overload_protection_low_v)
				{	
					Move(MoveSide::STOP);

					log_message = LogMessage::MOTOR_OVERLOAD;
					SendData(DataType::LOG_DATA);
					BuzzerTone(BuzzerSignal::WARNING);
					overload_protection_last_voltage_is_low = false;
				}
			}
			else
			{
				overload_protection_last_voltage_is_low = true;
				timer_overload_protection_low_v = millis() + overload_protection_low_v_time;
			}
		}
	}
}

float RoundFloatValueToTwoDigit(float value)
{
	return ((float)(int)(value * 100)) / 100;
}

void BuzzerTone(BuzzerSignal signal)
{
	Move(MoveSide::STOP);//function BuzzerTone hold one PWM signal for engine

	uint8_t sig = (uint8_t)signal;
	buzzer_is_tone = true;
	buzzer_current_signal_number = 0;
	buzzer_current_signal = signal;
	timer_buzzer = millis() + buzzer_time[sig][buzzer_current_signal_number];

	NewTone(pin_buzzer, buzzer_frequency[sig][buzzer_current_signal_number],buzzer_time[sig][buzzer_current_signal_number]);

	buzzer_current_signal_number++;
}

void BuzzerUpdate()
{
	if (buzzer_is_tone && millis() > timer_buzzer)
	{
		uint8_t sig = (uint8_t)buzzer_current_signal;

		NewTone(pin_buzzer, buzzer_frequency[sig][buzzer_current_signal_number],buzzer_time[sig][buzzer_current_signal_number]);

		if (buzzer_current_signal_number < 2)
		{
			timer_buzzer = millis() + buzzer_time[sig][buzzer_current_signal_number];
			buzzer_current_signal_number++;
		}
		else
		{
			buzzer_is_tone = false;
		}
	}
}

void InitSettings ()
{
	settings.protection_overload = true;
}