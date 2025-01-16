#include <Button.h>

#define mute_led 2
Button mute_btn(5);
#define deaf_led 3
Button deaf_btn(6);
#define mode_led 4
Button mode_btn(7);

const byte numChars = 4;
char receivedChars[numChars];   // an array to store the received data

struct btnState{
    int mute = 0;
    int deaf = 0;
    int mode = 0;
};

btnState state;

void setup() {

    mute_btn.begin();
    deaf_btn.begin();
    mode_btn.begin();

    pinMode(mute_led, OUTPUT);
    pinMode(deaf_led, OUTPUT);
    pinMode(mode_led, OUTPUT);

    digitalWrite(mute_led,1);
    digitalWrite(deaf_led,1);
    digitalWrite(mode_led,1);
    delay(1000);
    digitalWrite(mute_led,0);
    digitalWrite(deaf_led,0);
    digitalWrite(mode_led,0);

    Serial.begin(9600);
}

void loop() {
    ReceiveSerial();
    SendSerial();
}


void SendSerial(){
  if(mute_btn.pressed()){
    Serial.println("100");
  }
  if(deaf_btn.pressed()){
    Serial.println("010");
  }
  if(mode_btn.pressed()){
    Serial.println("001");
  }
}

void ReceiveSerial() {
    static byte ndx = 0;
    char endMarker = '\n';
    char rc;
    
    while (Serial.available() > 0) {
        rc = Serial.read();

        if (rc != endMarker) {
            receivedChars[ndx] = rc;
            ndx++;
            if (ndx >= numChars) {
                ndx = numChars - 1;
            }
        }
        else {
            receivedChars[ndx] = '\0'; // terminate the string
            ndx = 0;
            DisplayData();
        }
    }
}

int ConvertChar(char x){
  return (x == '1')?HIGH:LOW;
} 

void DisplayData() {
    digitalWrite(mute_led,ConvertChar(receivedChars[0]));
    digitalWrite(deaf_led,ConvertChar(receivedChars[1]));
    digitalWrite(mode_led,ConvertChar(receivedChars[2]));
}