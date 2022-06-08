#pragma once

//enum types
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

//struct types
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

struct ESP_SensorData
{
	float rpt_bat_vol;
	int rpt_bat_perc;
};