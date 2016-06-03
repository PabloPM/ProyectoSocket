using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Net;

namespace SocketServidor
{
    public partial class Form1 : Form
    {
		Servidor servidor;

        public Form1()
        {
            InitializeComponent();
			IPHostEntry ip = Dns.GetHostEntry (Dns.GetHostName ().ToString());
			listView1.View = View.Details;
			listView1.Columns.Add("Clientes Conectados", 200);
			servidor = new Servidor ();
			try{
				servidor.Escuchar();
				label1.Text = "IP:" + ip.AddressList [0].ToString ()+"| Puerto:"+servidor.Puerto;
				label2.Text = "Clientes conectados: " + servidor.Clientes ();
                servidor.envia += new Servidor.EnviarMensaje(ActualizaMensajes);
				servidor.actualiza += new Servidor.ActualizaLista(ActualizaLista);
				textBox1.Text = "Esperando clientes";
			}
			catch(Exception e){
				MessageBox.Show (e.ToString () ,"Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
				Environment.Exit (1);
			}
    	}

		public void ActualizaLista(string[] lista){
			if (this.listView1.InvokeRequired) {
				listView1.Invoke (new Servidor.ActualizaLista(ActualizaLista), new
					object[] { lista });
			}
			else{
                label2.Text = "Clientes conectados: " + servidor.Clientes();
                try
                {
                    ListViewItem item = new ListViewItem(lista);
                    //listView1.Items.Clear();
                    listView1.Items.Add(item);
                }
                catch (Exception e) { MessageBox.Show(e.ToString()); }
            }
		}


		public void ActualizaMensajes(string mensaje){
			if (this.textBox1.InvokeRequired) {
				textBox1.Invoke (new Servidor.EnviarMensaje(ActualizaMensajes), new
					object[] { mensaje });
			} else {
				textBox1.Text += mensaje;
				textBox1.ScrollToCaret();
				textBox2.Focus();
			}
		}

		// Eventos
        private void button1_Click(object sender, EventArgs e)
        {
			try{
				string[] ipPuerto = Regex.Split (listView1.SelectedItems[0].SubItems[0].Text, ":");
				IPEndPoint idCliente = new IPEndPoint (IPAddress.Parse(ipPuerto[0]), Convert.ToInt32(ipPuerto[1]));
				if (textBox2.Text.Length > 0) {
					servidor.EnviarDatos(idCliente, textBox2.Text);
					textBox1.ScrollToCaret ();
					textBox2.Clear();
				}
			}
			catch(Exception){ MessageBox.Show("Seleccione un cliente de la lista");}
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
			DialogResult respuesta = MessageBox.Show ("¿Desea cerrar el servidor?", "Aviso", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);
			if (respuesta == DialogResult.Yes) {
				try{
					servidor.EnviarDatos ("cerrar");
					Application.ExitThread ();
					Environment.Exit (0);
				}
				catch(Exception){
					Application.ExitThread ();
					Environment.Exit (0);
				}
			} 
			e.Cancel = !(respuesta == DialogResult.Yes);
        }
    }
}