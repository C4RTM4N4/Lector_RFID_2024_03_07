using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using Intermec.DataCollection.RFID;
using System.Windows.Forms;
using System.Collections;
using com.nem.aurawheel.Utils;

namespace rfidService
{

    struct tag
    {
        public DateTime timeStamp;
        public string data;
    }

    /// <summary>
    /// Autor:  Enrique Pulido
    /// Fecha:  30/10/2014 
    /// Nombre: rfidController
    /// Descripción:
    /// Clase para el control de lectores RFID intermec mediante TCP/IP
    /// 
    /// </summary>
    ///<remarks>
    /// <para>
    ///     <list></list>
    /// </para>
    ///</remarks>
    class rfidController
    {
        /// <summary>
        /// Estructura del Tag, en esta estructura se almacena la información de que se registra en cada una de las lecturas de tag
        /// </summary>
      
        static private Queue<tag> _readTags = new Queue<tag>();
        private BasicBRIReader RfidReader;
        static private bool _isConnected;
        static private int _ResponseTimeout;
        static tag tagRead = new tag();
       
        public tag[] readTags
        {
            
            get { return _readTags.ToArray(); }
    
        }
        public bool isConnected
        {
            get { return _isConnected;}
        }


        public rfidController(){
            //Buffer de 10KB y eventqueue de 200 eventos
            RfidReader = new BasicBRIReader(null, 10000, 200);
            //Dar de alta los gestores de eventos
            RfidReader.ReaderEvent += new BasicEvent(RfidReaderEvent);
            _isConnected = false;
            _ResponseTimeout = 10000;
        }
        public rfidController(int timeout)
        {
            //Buffer de 10KB y eventqueue de 200 eventos
            RfidReader = new BasicBRIReader(null, 10000, 200);
            //Dar de alta los gestores de eventos
            RfidReader.ReaderEvent += new BasicEvent(RfidReaderEvent);
            _isConnected = false;
            _ResponseTimeout = timeout;
        }
        /// <summary>
        /// Nombre: openConnetion
        /// Descripción:  Abre la conexion con el lector RFID. 
        /// Si ya hay una conexión abierta en primer lugar la cierra.
        /// </summary>
        /// <para name="ipAddress"> Dirección IP del lector</para>
        public void openConnection(string ipAddress, string port){

            try
            {
                if (_isConnected == true) RfidReader.Close();
                RfidReader.Open("tcp://" + ipAddress + ":" + port);
                RfidReader.Execute("VERSION", _ResponseTimeout);
                _isConnected = true;
            }
            catch (BasicReaderException ex)
            {
                //if (ex.ErrorCode == BasicReaderException.BEX_READER_ALREADY_OPEN)
                RfidReader.Close();
                throw ex;
            }
            catch (Exception ex)
            {
                throw ex;
            }

        }
        /// <summary>
        /// Nombre: openConnection
        /// Descripción:  Cierra la conexion con el lector RFID. 
        /// </summary>
        /// <para name="ipAddress"> Dirección IP del lector</para>
        public void closeConnection()
        {       
            try
            {
                _isConnected = false;
                RfidReader.Close();
                
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        /// <summary>
        /// Nombre: checkConnection
        /// Descripción:  comprueba la conexion con el equipo RFID
        /// </summary>
        /// <para name="ipAddress"> Dirección IP del lector</para>
        public void checkConnection()
        {
            try
            {
                RfidReader.Execute("VERSION", _ResponseTimeout);
            }
            catch (Exception ex)
            {
                RfidReader.Close();
                _isConnected = false;
                throw ex;
            }
        }
        /// <summary>
        /// Nombre: startReading
        /// Descripción:  El lector rfid se pone en modo lectura. 
        /// </summary>
        /// <para name=""> </para>
        public void startReading()
        {
            try
            {
                //_readTags.Clear(); Se borran el listado de tags 
                RfidReader.Execute("R REPORT=EVENT", _ResponseTimeout);
            }
            catch (Exception ex)
            {
                throw ex;
            }

        }
        /// <summary>
        /// Nombre: stopReading
        /// Descripción:  El lector rfid para de leer. 
        /// </summary>
        /// <para name=""> </para>
        public void stopReading()
        {
            try
            {
                RfidReader.Execute("R STOP", _ResponseTimeout);
                
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        /// <summary>
        /// Nombre: clearTagPoll
        /// Descripción: Borra el listado de tags leidos de la memoria del lector
        /// </summary>
        /// <para name=""> </para>
        public void clearTagPoll()
        {
            try
            {
                RfidReader.Execute("R POLL", _ResponseTimeout);

            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        /// <summary>
        /// Nombre: retunrTags
        /// Descripción:  Devuelve los tags leidos hasta una fecha indicada en ms 
        /// </summary>
        /// <paraname=""> </para>
        public tag[] returnTags(UInt64 maxTimestamp)
        {
            Queue<tag> resultTags = new Queue<tag>();

            try
            {
                //while ((UInt64)NemDateUtils.UniversalTimeMillis(_readTags.Peek().timeStamp) <= maxTimestamp)
                while (_readTags.Count > 0)
                {
                    UInt64 tagTimestamp = (UInt64)NemDateUtils.UniversalTimeMillis(_readTags.Peek().timeStamp);
                    /*lock (_readTags)
                    {
                        resultTags.Enqueue(_readTags.Dequeue());
                    }*/
                    
                    if (tagTimestamp <= maxTimestamp )
                    {
                        lock (_readTags)
                        {
                            resultTags.Enqueue(_readTags.Dequeue());
                        }
                    }
                    else
                    {
                        break;
                    }

                }
                return resultTags.ToArray();

            }
            catch (Exception ex)
            {
                throw ex;
            }
            
        }
        /// <summary>
        /// Nombre: RfidReader_ReaderEvent
        /// Descripción:  Gestor de eventos de la sesion BRI, se gestionan los eventos relacionados con lectura de tags y 
        /// activación de triggers.
        /// </summary>
        /// <para name="BasicEvtArgs"> Datos del evento causante</para>
        private static void RfidReaderEvent(object sender, BasicReaderEventArgs BasicEvtArgs){
           
            switch (BasicEvtArgs.EventType)
            {

                case BasicReaderEventArgs.EventTypes.EVT_TAG:
                    //Si el evento es de tipo tag, es decir se ha detectado un tag nuevo
                    tagRead.timeStamp = DateTime.Now;
  
                    //Se elimina la H del principio que indica que el numero es Hexadecimal
                    if (BasicEvtArgs.EventData.Length >= 25)
                    {
                        tagRead.data = BasicEvtArgs.EventData.Substring(1, 24);

                        lock (_readTags)
                        {
                            _readTags.Enqueue(tagRead);
                        }
                    }
                    break;
                default:
                    break;
            }
        }

    }
}
