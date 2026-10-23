#include <BLEDevice.h>
#include <BLEServer.h>
#include <BLEUtils.h>
#include <BLE2902.h>

#define VIB_MOTOR_PIN 13

unsigned long  motorOn = 0;

BLEServer* pServer;
BLECharacteristic* pCharacteristic;

#define SERVICE_UUID        "4fafc201-1fb5-459e-8fcc-c5c9c331914b"
#define CHARACTERISTIC_UUID "beb5483e-36e1-4688-b7f5-ea07361b26a8"

class MyServerCallbacks: public BLEServerCallbacks {
    void onConnect(BLEServer* pServer) {
      // do something when connected
      Serial.println("onConnect");
    };

    void onDisconnect(BLEServer* pServer) {
      // do something when disconnected
      ESP.restart();
    }
};

class MyCallbacks: public BLECharacteristicCallbacks {
    void onWrite(BLECharacteristic* pCharacteristic) {
        std::string value = pCharacteristic->getValue();

        if (value.length() > 0) {
            if (value[0] == '1') {
                //Serial.println("Received: 1");
                motorOn = millis() + 60;
                digitalWrite(VIB_MOTOR_PIN, HIGH);
                // 触发你的回调函数
            }
        }
    }
};

void setup() {
    Serial.begin(115200);
    Serial.println("Start");

    pinMode(VIB_MOTOR_PIN, OUTPUT);

    BLEDevice::init("MyESP32");
    pServer = BLEDevice::createServer();
    pServer->setCallbacks(new MyServerCallbacks());

    BLEService *pService = pServer->createService(SERVICE_UUID);
    pCharacteristic = pService->createCharacteristic(
                        CHARACTERISTIC_UUID,
                        BLECharacteristic::PROPERTY_READ |
                        BLECharacteristic::PROPERTY_WRITE
                      );

    pCharacteristic->setCallbacks(new MyCallbacks());

    pCharacteristic->addDescriptor(new BLE2902());

    pService->start();

    BLEAdvertising *pAdvertising = pServer->getAdvertising();
    pAdvertising->addServiceUUID(SERVICE_UUID);
    pAdvertising->setScanResponse(true);
    pAdvertising->start();
}

void loop() {
  if(motorOn < millis())
  {
    digitalWrite(VIB_MOTOR_PIN, LOW);
  }

  delay(1);
}


/*void loop() {
  // put your main code here, to run repeatedly:
  bool motorOn = false;
    for(int i=0; i<30; i++){
        motorOn = !motorOn;
        digitalWrite(VIB_MOTOR_PIN, motorOn);
        delay(500);
    }
}*/
