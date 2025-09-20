#include <ESP32Servo.h>

// -------- Pins --------
const int SERVO1_PIN = 21;   // Feed
const int SERVO2_PIN = 22;   // Nutrient

// Ultrasonic #1
const int TRIG_PIN1 = 5;
const int ECHO_PIN1 = 18;

// Ultrasonic #2
const int TRIG_PIN2 = 19;
const int ECHO_PIN2 = 23;

// pH (analog)
const int PH_PIN = 34;        // ADC1 channel

// -------- Servo angles --------
const int ANGLE_OPEN  = 90;
const int ANGLE_CLOSE = 0;

Servo servo1, servo2;
bool feedOpen = false, nutrOpen = false;

unsigned long lastSendMs = 0;
const unsigned long SEND_PERIOD_MS = 250;   // ~4Hz updates

// ---- Ultrasonic helper (returns cm, -1 if no echo) ----
long readUltrasonic(int trigPin, int echoPin) {
  digitalWrite(trigPin, LOW);  delayMicroseconds(2);
  digitalWrite(trigPin, HIGH); delayMicroseconds(10);
  digitalWrite(trigPin, LOW);

  long us = pulseIn(echoPin, HIGH, 30000);  // 30ms timeout
  if (us == 0) return -1;
  return (long)(us * 0.0343 / 2.0);         // cm
}

// ---- pH reading (very rough; calibrate for your probe!) ----
float readPH() {
  const int N = 10;
  uint32_t sum = 0;
  for (int i=0; i<N; ++i) { sum += analogRead(PH_PIN); delay(5); }
  float adc = sum / (float)N;            // 0..4095
  // Quick placeholder mapping -> 0..14 pH
  float ph = (adc / 4095.0f) * 14.0f;    // replace with your calibration
  return ph;
}

void setup() {
  Serial.begin(115200);

  servo1.attach(SERVO1_PIN);
  servo2.attach(SERVO2_PIN);
  servo1.write(ANGLE_CLOSE);
  servo2.write(ANGLE_CLOSE);

  pinMode(TRIG_PIN1, OUTPUT); pinMode(ECHO_PIN1, INPUT);
  pinMode(TRIG_PIN2, OUTPUT); pinMode(ECHO_PIN2, INPUT);
}

void loop() {
  // --- Serial commands for servos ---
  while (Serial.available() > 0) {
    char c = (char)Serial.read();
    switch (c) {
      case 'a': servo1.write(ANGLE_OPEN);  feedOpen = true;  break;
      case 'b': servo1.write(ANGLE_CLOSE); feedOpen = false; break;
      case 'c': servo2.write(ANGLE_OPEN);  nutrOpen = true;  break;
      case 'd': servo2.write(ANGLE_CLOSE); nutrOpen = false; break;
    }
  }

  // --- Periodic sensor output ---
  unsigned long now = millis();
  if (now - lastSendMs >= SEND_PERIOD_MS) {
    lastSendMs = now;

    long d1 = readUltrasonic(TRIG_PIN1, ECHO_PIN1); if (d1 < 0) d1 = 0;
    long d2 = readUltrasonic(TRIG_PIN2, ECHO_PIN2); if (d2 < 0) d2 = 0;
    float ph = readPH();

    // Labels the VB app expects (each on its own line)
    Serial.print("DIST1="); Serial.println((int)d1);
    Serial.print("DIST2="); Serial.println((int)d2);
    Serial.print("PH=");    Serial.println(ph, 2);
  }
}
