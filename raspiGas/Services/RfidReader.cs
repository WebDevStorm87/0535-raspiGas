using System.IO.Ports;

namespace raspiGas.Services;


public class RfidReader : BackgroundService
{
	private readonly ILogger<RfidReader> _logger;
	private string _rfid;
	private string _lastRfid;
	static SerialPort _serialPort;
	
	public RfidReader(ILogger<RfidReader> logger)
	{
		_logger = logger;
		_serialPort = new SerialPort();
			
		_serialPort.PortName = "/dev/ttyAMA0";
		_serialPort.BaudRate = 9600;
		_serialPort.DataBits = 8;
		_serialPort.StopBits = StopBits.One;
		_serialPort.Parity = Parity.None;

	}

	public bool OpenPort()
	{
		if (_serialPort == null)
			return false;
		
		_logger.LogInformation("OperPortSerial");
		_serialPort.Open();
		return _serialPort.IsOpen;
	}
	
	public string ReadData()
    {
      int bToRead = _serialPort.BytesToRead;
      _logger.LogInformation(bToRead.ToString());
      byte[] bufferRead = new byte[bToRead];
      byte[] bufferRfidCode = new byte[10];
      byte[] bufferRfidCs = new byte[2];
      string rxStringRfidCode = "";
      string rxStringAsciiChecksum = "";
      string appRead = "";

      bool readStart = false;
      bool readComplete = false;

      _serialPort.Read(bufferRead, 0, bToRead);


      for (int n=0; n<bufferRead.Length;n++)
      {

        appRead += bufferRead[n].ToString();
      }


      //v.vLog.writeLog(appRead, "myLog");

      int p = 0;

      for(int n=0; n< bufferRead.Length; n++)
      {
        if (readStart && !readComplete)
        {
          //rxString += bufferRead[n].ToString();
          if (p < 10)
            bufferRfidCode[p] = bufferRead[n];

          else if (p>= 10 && p<=11)
            bufferRfidCs[p-10] = bufferRead[n];
          p++;
        }

        if (bufferRead[n] == 0x02)
          readStart = true;

        if (bufferRead[n] == 0x03)
        {
          readComplete = true;
        }
      }

      //rxStringExaChecksum = BitConverter.ToString(bufferRfidCs);
      rxStringRfidCode = System.Text.Encoding.ASCII.GetString(bufferRfidCode);
      rxStringAsciiChecksum = System.Text.Encoding.ASCII.GetString(bufferRfidCs);
      bool checkSumOk = false;

      try
      {
        byte[] pippo = new byte[5];

        byte cs = 0;
        for (int n = 0; n < 5; n++)
        {
          pippo[n] = byte.Parse(rxStringRfidCode.Substring(n * 2, 2), System.Globalization.NumberStyles.HexNumber);
          cs ^= pippo[n];
        }
        checkSumOk = (cs == byte.Parse(rxStringAsciiChecksum, System.Globalization.NumberStyles.HexNumber));
      }
      catch
      {
        _logger.LogCritical("Errore in trychatch checksum", "evenLog");
      }

      #region commentato

      //rxString = tmpByte.ToString();

      //while (tmpByte != 255)
      //{
      //  rxString += ((char)tmpByte);
      //  tmpByte = (byte)sPort.ReadByte();
      //}
      //if (v.n0CallBack != null)
      //v.n0CallBack(6, "l'rFid letto è read: " + rxString + " Exa: "  + rxStringExa + " ascii: " + rxStringAscii
      //+ " ExaChecksum: " + rxStringExaChecksum + " asciiChecksum: " + rxStringAsciiChecksum + " il confronto della cs restituisce: " + gigio.ToString());
      #endregion
      string result = "";
      if (checkSumOk && p>=12)
      { result = rxStringRfidCode; }
      else
      { result = ""; }



      return result;
    }
	
	
	
	protected override async Task ExecuteAsync(CancellationToken stoppingToken)
	{
		while (!stoppingToken.IsCancellationRequested)
		{
			_logger.LogInformation("ExecuteAsync ReadRfid");
			await Task.Delay(100, stoppingToken);
			if (!_serialPort.IsOpen)
			{
				OpenPort();
				await Task.Delay(1500, stoppingToken);
			}
			
			
			if (_rfid != "" && _rfid != _lastRfid)
			{
				_logger.LogInformation(_rfid);
				_lastRfid = _rfid;
				_serialPort.DiscardInBuffer();
				_serialPort.DiscardOutBuffer();
				_serialPort.ReadExisting();
			}

			try
			{
				_rfid = ReadData();
			}
			catch (Exception e)
			{
				_logger.LogError(e.ToString());
				throw;
			}
			
			
		}
		_serialPort.Close();
	}
}