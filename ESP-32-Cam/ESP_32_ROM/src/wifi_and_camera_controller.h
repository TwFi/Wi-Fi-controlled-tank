#include <Arduino.h>
#include "esp_camera.h"
#include "WiFi.h"
#include "img_converters.h"
#include "esp_http_server.h"
#include "esp_wifi.h"

extern void BuzzerTone(BuzzerSignal sig);
extern MoveSide move_side;
extern void TransferDataToNano(DataType data_type);
extern bool WDT_is_on;
extern void DisableWDT();
extern void ComboLog(String message);

IPAddress repeater_ip(IP_REPEATER_MODE);
IPAddress repeater_gw(REPEATER_GATEWAY);
IPAddress self_ip(IP_SELF_AP_MODE);
IPAddress net(NETMASK);

bool wifi_AP = false;
unsigned long timer_wifi_watchdog = 0;
sensor_t *camera_sensor2 = NULL;
#define PART_BOUNDARY "123456789000000000000987654321"
static const char *_STREAM_CONTENT_TYPE = "multipart/x-mixed-replace;boundary=" PART_BOUNDARY;
static const char *_STREAM_BOUNDARY = "\r\n--" PART_BOUNDARY "\r\n";
static const char *_STREAM_PART = "Content-Type: image/jpeg\r\nContent-Length: %u\r\n\r\n";
httpd_handle_t stream_httpd = NULL;
framesize_t current_cam_framesize;
int current_cam_quality;
gainceiling_t current_cam_gain;

static esp_err_t stream_handler(httpd_req_t *req)
{
  camera_fb_t *fb = NULL;
  esp_err_t res = ESP_OK;
  size_t _jpg_buf_len = 0;
  uint8_t *_jpg_buf = NULL;
  char *part_buf[64];

  res = httpd_resp_set_type(req, _STREAM_CONTENT_TYPE);
  if (res != ESP_OK)
  {
    return res;
  }

  while (true)
  {
    fb = esp_camera_fb_get();
    if (!fb)
    {
      if (DEBUG_SERIAL)
      {
        Serial.println(F("Stream frame capture failed!"));
      }
      res = ESP_FAIL;
    }
    else
    {
      if (fb->width > 400)
      {
        if (fb->format != PIXFORMAT_JPEG)
        {
          bool jpeg_converted = frame2jpg(fb, 80, &_jpg_buf, &_jpg_buf_len);
          esp_camera_fb_return(fb);
          fb = NULL;
          if (!jpeg_converted)
          {
            if (DEBUG_SERIAL)
            {
              Serial.println(F("JPEG compression failed"));
            }
            res = ESP_FAIL;
          }
        }
        else
        {
          _jpg_buf_len = fb->len;
          _jpg_buf = fb->buf;
        }
      }
    }
    if (res == ESP_OK)
    {
      res = httpd_resp_send_chunk(req, _STREAM_BOUNDARY, strlen(_STREAM_BOUNDARY));
    }
    if (res == ESP_OK)
    {
      size_t hlen = snprintf((char *)part_buf, 64, _STREAM_PART, _jpg_buf_len);
      res = httpd_resp_send_chunk(req, (const char *)part_buf, hlen);
    }
    if (res == ESP_OK)
    {
      res = httpd_resp_send_chunk(req, (const char *)_jpg_buf, _jpg_buf_len);
    }
    if (fb)
    {
      esp_camera_fb_return(fb);
      fb = NULL;
      _jpg_buf = NULL;
    }
    else if (_jpg_buf)
    {
      free(_jpg_buf);
      _jpg_buf = NULL;
    }
    if (res != ESP_OK)
    {
      break;
    }
  }
  return res;
}

void startCameraServer()
{
  httpd_config_t config = HTTPD_DEFAULT_CONFIG();
  config.server_port = STREAM_PORT;

  httpd_uri_t index_uri = {
      .uri = "/",
      .method = HTTP_GET,
      .handler = stream_handler,
      .user_ctx = NULL};

  if (DEBUG_SERIAL)
  {
    Serial.printf("Starting webcam server on port: '%d'\n", config.server_port);
  }
  if (httpd_start(&stream_httpd, &config) == ESP_OK)
  {
    httpd_register_uri_handler(stream_httpd, &index_uri);
  }
}

void WiFiTryToConnect()
{
  if (WDT_is_on)
  {
    DisableWDT();
  }

  WiFi.disconnect();
  WiFi.mode(WIFI_STA);
  WiFi.begin(REPEATER_AP_SSID, AP_PASSWORD);
  WiFi.config(repeater_ip, repeater_gw, net);

  unsigned long start = millis();
  while ((millis() - start <= WIFI_CONNECTING_TIME) && WiFi.status() != WL_CONNECTED)
  {
    if (DEBUG_SERIAL)
    {
      Serial.print(".");
    }
    delay(500);
  }

  if (WiFi.status() != WL_CONNECTED)
  {
    wifi_AP = true;

    if (DEBUG_SERIAL)
    {
      Serial.println(F("Repeater not found – switching to AP mode!"));
    }
  }
  else if (WiFi.status() == WL_CONNECTED)
  {
    //Disable power saving on WiFi to improve responsiveness
    WiFi.setSleep(false);

    if (DEBUG_SERIAL)
    {
      Serial.println(F("Repeater found – connection succeeded!"));
    }
  }

  if (wifi_AP)
  {
    WiFi.disconnect();
    WiFi.mode(WIFI_AP);
    esp_wifi_set_protocol(WIFI_IF_AP, WIFI_PROTOCOL_11B);
    //esp_wifi_set_bandwidth(WIFI_IF_AP, WIFI_BW_HT20);
    WiFi.softAP(SELF_AP_SSID, AP_PASSWORD, AP_CHANNEL);
    delay(500); //wait SYSTEM_EVENT_AP_START
    WiFi.softAPConfig(self_ip, self_ip, net);
  }

  timer_wifi_watchdog = millis() + WIFI_WATCHDOG_TIME;
}

void WiFiInit()
{
  if (DEBUG_SERIAL)
  {
    byte mac[6] = {0, 0, 0, 0, 0, 0};
    WiFi.macAddress(mac);
    Serial.printf("MAC address: %02X:%02X:%02X:%02X:%02X:%02X\r\n", mac[0], mac[1], mac[2], mac[3], mac[4], mac[5]);
  }

  WiFiTryToConnect();
}

void CameraInit()
{
  // Create camera config structure; and populate with hardware and other defaults
  camera_config_t config;
  config.ledc_channel = LEDC_CHANNEL_0;
  config.ledc_timer = LEDC_TIMER_0;
  config.pin_d0 = Y2_GPIO_NUM;
  config.pin_d1 = Y3_GPIO_NUM;
  config.pin_d2 = Y4_GPIO_NUM;
  config.pin_d3 = Y5_GPIO_NUM;
  config.pin_d4 = Y6_GPIO_NUM;
  config.pin_d5 = Y7_GPIO_NUM;
  config.pin_d6 = Y8_GPIO_NUM;
  config.pin_d7 = Y9_GPIO_NUM;
  config.pin_xclk = XCLK_GPIO_NUM;
  config.pin_pclk = PCLK_GPIO_NUM;
  config.pin_vsync = VSYNC_GPIO_NUM;
  config.pin_href = HREF_GPIO_NUM;
  config.pin_sscb_sda = SIOD_GPIO_NUM;
  config.pin_sscb_scl = SIOC_GPIO_NUM;
  config.pin_pwdn = PWDN_GPIO_NUM;
  config.pin_reset = RESET_GPIO_NUM;
  config.xclk_freq_hz = 20000000;
  config.pixel_format = PIXFORMAT_JPEG;
  config.fb_count = 2;

  current_cam_framesize = FRAMESIZE_HVGA;
  current_cam_quality = 20; //10-63 lower number means higher quality
  current_cam_gain = (gainceiling_t)0;

  config.frame_size = current_cam_framesize;
  config.jpeg_quality = current_cam_quality; 
  
  esp_err_t err = esp_camera_init(&config);
  if (err != ESP_OK)
  {
    BuzzerTone(BuzzerSignal::ERROR);
    if (DEBUG_SERIAL)
    {
      //Serial.printf("Camera init failed with error 0x%x", err);
      Serial.println(F("Camera init failed"));
    }
    ESP.restart();
  }
  else
  {
    if (DEBUG_SERIAL)
    {
     Serial.println(F("Camera init succeeded"));
    }
    camera_sensor2 = esp_camera_sensor_get();
  }

  startCameraServer();
}

void WiFiReconnect()
{
  move_side = MoveSide::STOP;
  TransferDataToNano(DataType::CONTROL_DATA);
  WiFiTryToConnect();
}

void SetCameraFramesize(int size)
{
  if (size < 0 || size > 13)
  {
    ComboLog(F("Not supported framesize!"));
    return;
  }

  current_cam_framesize = (framesize_t)size;
  camera_sensor2->set_framesize(camera_sensor2, current_cam_framesize); 
}

void SetCameraQuality(int quality) 
{
  if (quality < 10 || quality > 63)
  {
    ComboLog(F("Not supported quality!"));
    return;
  }

  current_cam_quality = quality;
  camera_sensor2->set_quality(camera_sensor2, current_cam_quality);
}

void SetCameraGain(int gain) 
{
  if (gain < 0 || gain > 6)
  {
    ComboLog(F("Not supported gain!"));
    return;
  }

  current_cam_gain = (gainceiling_t)gain;
  camera_sensor2->set_gainceiling(camera_sensor2, current_cam_gain);
}

void GetCameraSettings(int *frame, int *quality, int *gain)
{
  *frame = (int)current_cam_framesize;
  *quality = current_cam_quality;
  *gain = (int)current_cam_gain;
}

void WiFiUpdate()
{
  if (millis() > timer_wifi_watchdog)
  {
    timer_wifi_watchdog = millis() + WIFI_WATCHDOG_TIME;

    if (!wifi_AP && WiFi.status() != WL_CONNECTED)
    {
      BuzzerTone(BuzzerSignal::ERROR);
      Serial.println(F("WiFi not connected"));
      WiFiReconnect();
    }
  }
}