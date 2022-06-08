#define DEBUG_SERIAL 					false

#define SELF_AP_SSID 					"WiFi Tank"
#define REPEATER_AP_SSID                "Tank Repeater"
#define AP_PASSWORD 	                "00000000"
#define AP_CHANNEL 						11

#define IP_REPEATER_MODE   				192,168,4,2
#define REPEATER_GATEWAY 				192,168,4,1
#define IP_SELF_AP_MODE                 192,168,6,1
#define NETMASK 						255,255,255,0

#define STREAM_PORT 					1051 //camera stream
#define DATA_HTTP_SERVER_PORT			1053 //http and websocket server for data transmission

#define SERIAL_SPEED 					115200
#define SERIAL_1_SPEED                  76800 //UART transfer

#define WIFI_CONNECTING_TIME            3000
#define WIFI_WATCHDOG_TIME              1000


/*
 * Camera Hardware Selectiom
 * You must uncomment one, and only one, of the lines below to select your board model.
 * This is not optional
 */
#define CAMERA_MODEL_AI_THINKER 
// #define CAMERA_MODEL_WROVER_KIT
// #define CAMERA_MODEL_ESP_EYE
// #define CAMERA_MODEL_M5STACK_PSRAM
// #define CAMERA_MODEL_M5STACK_V2_PSRAM
// #define CAMERA_MODEL_M5STACK_WIDE
// #define CAMERA_MODEL_M5STACK_ESP32CAM 
// #define CAMERA_MODEL_TTGO_T_JOURNAL
