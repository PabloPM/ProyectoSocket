using System;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Collections;


namespace SocketServidor
{
	public class Servidor
	{
		private struct InformacionCLiente
		{
			public Socket socket;
			public Thread hilo;
			public string UltimoMensaje;
		}
		private int puerto = 8081;
		private TcpListener tcpListener;
		private Hashtable clientes = new Hashtable();
		private Thread hiloTCP;
		private IPEndPoint idCliente;

		public delegate void EnviarMensaje(string mensaje);
		public event EnviarMensaje envia;

		public delegate void ActualizaLista(string[] lista);
		public event ActualizaLista actualiza;

		public int Puerto {
			get {
				return this.puerto;
			}
		}

		public int Clientes(){
			return clientes.Count;
		}

		private string[] ListaUsuarios(){
            IPEndPoint[] usuarios = new IPEndPoint[clientes.Count];
            string [] lista = new string[clientes.Count];
            this.clientes.Keys.CopyTo (usuarios, 0);
            lista = Array.ConvertAll<IPEndPoint, string>(usuarios, Convertir);
			return lista;
		}

        private string Convertir(IPEndPoint cliente) {
            return (cliente == null) ? string.Empty : cliente.ToString();
        }

		public void Escuchar()
		{
			tcpListener = new TcpListener (Convert.ToInt32(puerto));
			tcpListener.Start ();

			hiloTCP = new Thread (new ThreadStart(EsperarCliente));
			hiloTCP.Start ();
		}



		private void EsperarCliente() { 
			InformacionCLiente cliente = new InformacionCLiente ();
			while (true) 
			{
				cliente.socket = tcpListener.AcceptSocket ();
				this.idCliente = (IPEndPoint)cliente.socket.RemoteEndPoint;
				cliente.hilo = new Thread (new ThreadStart (LeerSocket));
				cliente.hilo.Start ();
				lock (this) 
				{
					try{ 
						this.clientes.Add (this.idCliente, cliente);
						envia("\nUn cliente ha iniciado: " + idCliente);
						EnviarDatos(this.idCliente, "Bienvenido");
						actualiza(ListaUsuarios());
					}
					catch(Exception e){ MessageBox.Show (e.ToString ());}
				}
			}
		}

		private void LeerSocket()
		{
			IPEndPoint idReal;
			byte[] recibir;
			InformacionCLiente cliente;
			int retorno = 0;
			idReal = this.idCliente;
			cliente = (InformacionCLiente) clientes[idReal];
			while (true) 
			{
				if (cliente.socket.Connected) 
				{
					try
					{
						recibir = new byte[256];
						retorno = cliente.socket.Receive(recibir, recibir.Length, SocketFlags.None);
						if(retorno > 0)
						{
							cliente.UltimoMensaje = Encoding.UTF8.GetString(recibir, 0, recibir.Length);
							clientes[idReal] = cliente;
							envia("\n"+idReal+": "+cliente.UltimoMensaje);
						}
						else{
							try{
								this.clientes.Remove(this.idCliente);
								envia("\n"+idCliente+" se ha desconectado");
								actualiza(ListaUsuarios());
							}
							catch(Exception e){ MessageBox.Show (e.ToString ());}
							break;
						}
					}
					catch(Exception e) {
						MessageBox.Show (e.ToString());
					}
				}
			}
			CerrarHilo (idReal);
		}


		private void CerrarHilo(IPEndPoint idCliente)
		{
			InformacionCLiente cliente;
			try{
				cliente = (InformacionCLiente)clientes [idCliente];
				cliente.hilo.Abort();
			}
			catch(Exception){
				lock (this) {
					clientes.Remove (idCliente);
				}
			}
		}

		private string ObtenerDatos(IPEndPoint idCliente)
		{
			InformacionCLiente cliente;
			cliente = (InformacionCLiente)clientes [idCliente];
			return cliente.UltimoMensaje;
		}

		public void EnviarDatos(string datos)
		{
			foreach(InformacionCLiente cliente in clientes.Values)
				EnviarDatos((IPEndPoint) cliente.socket.RemoteEndPoint, datos);
		}

		public void EnviarDatos (IPEndPoint idCliente, string datos)
		{
			InformacionCLiente cliente;
			cliente = (InformacionCLiente)clientes [idCliente];

			int logitudDeBytes = Encoding.UTF8.GetByteCount (datos);
			byte[] bytesAEnviar = Encoding.UTF8.GetBytes (datos);
			byte[] longitudDeBytesAEnviar = BitConverter.GetBytes (logitudDeBytes);
			cliente.socket.Send (longitudDeBytesAEnviar);
			cliente.socket.Send (bytesAEnviar);
			envia("\nServidor-"+idCliente.ToString()+":" + datos);
		}

		public void Cerrar ()
		{
			foreach (InformacionCLiente cliente in clientes.Values)
				Cerrar ((IPEndPoint) cliente.socket.RemoteEndPoint);
		}

		public void Cerrar(IPEndPoint idCliente)
		{
			InformacionCLiente cliente;
			cliente = (InformacionCLiente)clientes [idCliente];
			cliente.socket.Close ();
			CerrarHilo (idCliente);
		}

	}
}

