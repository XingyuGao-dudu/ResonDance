using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class BleThing : MonoBehaviour
{
    public string DeviceName = "MyESP32";
    public string ServiceUUID = "4fafc201-1fb5-459e-8fcc-c5c9c331914b";
    public string LedUUID = "beb5483e-36e1-4688-b7f5-ea07361b26a8";

    enum States
    {
        None,
        Scan,
        ScanRSSI,
        ReadRSSI,
        Connect,
        RequestMTU,
        Subscribe,
        Unsubscribe,
        Disconnect,
    }

    private bool _connected = false;
    private float _timeout = 0f;
    private States _state = States.None;
    private string _deviceAddress;
    private bool _foundLedUUID = false;
    private bool _rssiOnly = false;
    private int _rssi = 0;

    public Text StatusText;

    private string StatusMessage
    {
        set
        {
            BluetoothLEHardwareInterface.Log(value);
            StatusText.text = value;
        }
    }

    void Reset()
    {
        _connected = false;
        _timeout = 0f;
        _state = States.None;
        _deviceAddress = null;
        _foundLedUUID = false;
        _rssi = 0;
    }

    void SetState(States newState, float timeout)
    {
        _state = newState;
        _timeout = timeout;
    }

    void StartProcess()
    {
        Reset();
        BluetoothLEHardwareInterface.Initialize(true, false, () =>
        {

            SetState(States.Scan, 0.1f);

        }, (error) =>
        {

            StatusMessage = "Error during initialize: " + error;
        });
    }

    // Start is called before the first frame update
    void Start()
    {
        StartProcess();
    }

    // Update is called once per frame
    void Update()
    {
        if (_timeout > 0f)
        {
            _timeout -= Time.deltaTime;
            if (_timeout <= 0f)
            {
                _timeout = 0f;

                switch (_state)
                {
                    case States.None:
                        break;

                    case States.Scan:
                        StatusMessage = "Scanning for " + DeviceName;

                        BluetoothLEHardwareInterface.ScanForPeripheralsWithServices(null, (address, name) =>
                        {
                            // if your device does not advertise the rssi and manufacturer specific data
                            // then you must use this callback because the next callback only gets called
                            // if you have manufacturer specific data

                            if (!_rssiOnly)
                            {
                                if (name.Contains(DeviceName))
                                {
                                    StatusMessage = "Found " + name;

                                    // found a device with the name we want
                                    // this example does not deal with finding more than one
                                    _deviceAddress = address;
                                    SetState(States.Connect, 0.5f);
                                }
                            }

                        }, (address, name, rssi, bytes) =>
                        {

                            // use this one if the device responses with manufacturer specific data and the rssi

                            if (name.Contains(DeviceName))
                            {
                                StatusMessage = "Found " + name;

                                if (_rssiOnly)
                                {
                                    _rssi = rssi;
                                }
                                else
                                {
                                    // found a device with the name we want
                                    // this example does not deal with finding more than one
                                    _deviceAddress = address;
                                    SetState(States.Connect, 0.5f);
                                }
                            }

                        }, _rssiOnly); // this last setting allows RFduino to send RSSI without having manufacturer data

                        if (_rssiOnly)
                            SetState(States.ScanRSSI, 0.5f);
                        break;

                    case States.ScanRSSI:
                        break;

                    case States.ReadRSSI:
                        StatusMessage = $"Call Read RSSI";
                        BluetoothLEHardwareInterface.ReadRSSI(_deviceAddress, (address, rssi) =>
                        {
                            StatusMessage = $"Read RSSI: {rssi}";
                        });

                        SetState(States.ReadRSSI, 2f);
                        break;

                    case States.Connect:
                        StatusMessage = "Connecting...";

                        // set these flags
                        _foundLedUUID = false;

                        // note that the first parameter is the address, not the name. I have not fixed this because
                        // of backwards compatiblity.
                        // also note that I am note using the first 2 callbacks. If you are not looking for specific characteristics you can use one of
                        // the first 2, but keep in mind that the device will enumerate everything and so you will want to have a timeout
                        // large enough that it will be finished enumerating before you try to subscribe or do any other operations.
                        BluetoothLEHardwareInterface.ConnectToPeripheral(_deviceAddress, null, null, (address, serviceUUID, characteristicUUID) =>
                        {
                            StatusMessage = "Connected...";

                            BluetoothLEHardwareInterface.StopScan();

                            if (IsEqual(serviceUUID, ServiceUUID))
                            {
                                StatusMessage = "Found Service UUID";

                                _foundLedUUID = _foundLedUUID || IsEqual(characteristicUUID, LedUUID);

                                // if we have found both characteristics that we are waiting for
                                // set the state. make sure there is enough timeout that if the
                                // device is still enumerating other characteristics it finishes
                                // before we try to subscribe
                                if (_foundLedUUID)
                                {
                                    _connected = true;
                                    SetState(States.RequestMTU, 2f);
                                }
                            }
                        });
                        break;

                    case States.RequestMTU:
                        StatusMessage = "Requesting MTU";

                        BluetoothLEHardwareInterface.RequestMtu(_deviceAddress, 185, (address, newMTU) =>
                        {
                            StatusMessage = "MTU set to " + newMTU.ToString();

                            SetState(States.ReadRSSI, 0.1f);
                        });
                        break;

                    case States.Unsubscribe:
                        //BluetoothLEHardwareInterface.UnSubscribeCharacteristic(_deviceAddress, ServiceUUID, ButtonUUID, null);
                        SetState(States.Disconnect, 4f);
                        break;

                    case States.Disconnect:
                        StatusMessage = "Commanded disconnect.";

                        if (_connected)
                        {
                            BluetoothLEHardwareInterface.DisconnectPeripheral(_deviceAddress, (address) =>
                            {
                                StatusMessage = "Device disconnected";
                                BluetoothLEHardwareInterface.DeInitialize(() =>
                                {
                                    _connected = false;
                                    _state = States.None;
                                });
                            });
                        }
                        else
                        {
                            BluetoothLEHardwareInterface.DeInitialize(() =>
                            {
                                _state = States.None;
                            });
                        }
                        break;
                }
            }
        }
    }

    string FullUUID(string uuid)
    {
        string fullUUID = uuid;
        if (fullUUID.Length == 4)
            fullUUID = "0000" + uuid + "-0000-1000-8000-00805f9b34fb";

        return fullUUID;
    }

    bool IsEqual(string uuid1, string uuid2)
    {
        if (uuid1.Length == 4)
            uuid1 = FullUUID(uuid1);
        if (uuid2.Length == 4)
            uuid2 = FullUUID(uuid2);

        return (uuid1.ToUpper().Equals(uuid2.ToUpper()));
    }

    void SendByte(byte value)
    {
        byte[] data = { value };
        BluetoothLEHardwareInterface.WriteCharacteristic(_deviceAddress, ServiceUUID, LedUUID, data, data.Length, true, (characteristicUUID) =>
        {

            BluetoothLEHardwareInterface.Log("Write Succeeded");
        });
    }

    public void OnLED()
    {
        SendByte((byte)'1');
    }
}
