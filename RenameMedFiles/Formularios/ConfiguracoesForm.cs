using RenameMedFiles.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml.Linq;

namespace RenameMedFiles
{
    public partial class ConfiguracoesForm : Form
    {
        BindingList<Evento> bindingList;

        public ConfiguracoesForm()
        {
            InitializeComponent();

            dataEventos.AllowUserToAddRows = true;
            LoadData();
        }



        private void LoadData()
        {
            if (!File.Exists(@"C:\SIGAMDT\Config.xml"))
                return;

            XDocument xml = XDocument.Load(@"C:\SIGAMDT\Config.xml");

            var eventos = xml.Root.Elements("Evento")
                .Select(x => new Evento
                {
                    Codigo = (string)x.Attribute("value"),
                    Descricao = (string)x.Attribute("description"),
                }).ToList();

            var con = xml.Root.Elements("ConnectionString")
                .Select(x => new Configuracao
                {
                    ConnectionString = (string)x.Attribute("connectionString"),
                    TabelaSRA = (string)x.Attribute("tabelaSra"),
                    TabelaC9V = (string)x.Attribute("tabelaC9V")
                })
                .FirstOrDefault();

            var token = xml.Root.Elements("Token")
               .Select(x => (string)x.Attribute("value"))
               .FirstOrDefault();

            textBoxToken.Text = token;

            connectionString.Text = con.ConnectionString;
            tabelaSRA.Text = con.TabelaSRA;
            textC9Vtabela.Text = con.TabelaC9V;

            bindingList = new BindingList<Evento>(eventos);
            dataEventos.DataSource = bindingList;
           
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (!Directory.Exists(@"C:\SIGAMDT"))
            {
                Directory.CreateDirectory(@"C:\SIGAMDT");
            }
            if (File.Exists(@"C:\SIGAMDT\Config.xml"))
                File.Delete(@"C:\SIGAMDT\Config.xml");



            if (!File.Exists(@"C:\SIGAMDT\Config.xml"))
            {
                var texto = $@"<?xml version=""1.0"" encoding=""UTF-8""?>
                            <configuration>
                                <ConnectionString name=""conBD_Protheus"" connectionString=""{connectionString.Text}"" tabelaSra=""{tabelaSRA.Text}""
                                tabelaC9V=""{textC9Vtabela.Text}""/>
                                <Token value=""{textBoxToken.Text}""/>";


                foreach (Evento eve in (BindingList<Evento>)dataEventos.DataSource)
                {
                    texto += $@"<Evento name=""evento"" value=""{eve.Codigo}"" description=""{eve.Descricao}""/>";
                }


                texto += "</configuration>";


                byte[] textoBytes = Encoding.UTF8.GetBytes(texto);

                using (FileStream arquivo = File.Create(@"C:\SIGAMDT\Config.xml", texto.Length))
                {
                    arquivo.Write(textoBytes);
                }
            }
            this.Close();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {

        }
    }
}
