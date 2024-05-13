using Dapper;
using RenameMedFiles.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml;
using System.Xml.Linq;

namespace RenameMedFiles.Formularios
{
    public partial class RenameForm : Form
    {
        Configuracao Configuracao { get; set; }
        IList<Evento> Eventos { get; set; }

        public RenameForm()
        {
            InitializeComponent();
            btnPropriedade.Click += PropriedadesOnIni;

            while (!CheckOnInit())
            {
                new ConfiguracoesForm().ShowDialog();
            }

            CarregarParametros();

        }

        private void CarregarParametros()
        {
            XDocument cofing = XDocument.Load(@"C:\SIGAMDT\Config.xml");

            Eventos = cofing.Root.Elements("Evento")
                 .Select(x => new Evento
                 {
                     Codigo = (string)x.Attribute("value"),
                     Descricao = (string)x.Attribute("description"),
                 })
                 .ToList();


            Configuracao = cofing.Root.Elements("ConnectionString")
                .Select(x => new Configuracao
                {
                    ConnectionString = (string)x.Attribute("connectionString"),
                    TabelaSRA = (string)x.Attribute("tabelaSra"),
                    TabelaC9V = (string)x.Attribute("tabelaC9V")
                }).FirstOrDefault();

            var token = cofing.Root.Elements("Token")
                .Select(x => (string)x.Attribute("value"))
                .FirstOrDefault();

            Configuracao.Token = token;
            if (!Configuracao.IsValidToken)
            {
                MessageBox.Show("Token informado inválido", "Produto não ativado", MessageBoxButtons.OK);
                btnAbrirPasta.Enabled = false;
                btnProcessar.Enabled = false;
                textEvento.Enabled = false;
            }


            textEvento.Items.Clear();

            foreach (var e in Eventos)
            {
                textEvento.Items.Add(e.Codigo);
            }
        }

        private bool CheckOnInit()
        {
            if (!Directory.Exists(@"C:\SIGAMDT"))
            {
                return false;
            }
            if (!File.Exists(@"C:\SIGAMDT\Config.xml"))
            {
                return false;
            }

            return true;
        }

        private void PropriedadesOnIni(object sender, EventArgs e)
        {
            new ConfiguracoesForm().ShowDialog();
            CarregarParametros();
        }

        private void button1_Click(object sender, EventArgs e)
        {

            this.folderDialog.ShowDialog();
            if (string.IsNullOrEmpty(folderDialog.SelectedPath))
            {
                return;
            }

            var listaDearquivos = Directory.EnumerateFiles(folderDialog.SelectedPath);
            listaDearquivos.ToList()
                .ForEach(item => lstArquivos.Items.Add(item));


        }

        private void toolStrip1_ItemClicked(object sender, ToolStripItemClickedEventArgs e)
        {

        }

        private void button1_MouseHover(object sender, EventArgs e)
        {
            labelInfo.Text = "Informe a pasta onde estão os arquivos XML a renomear";
        }

        private void textEvento_MouseHover(object sender, EventArgs e)
        {
            labelInfo.Text = "Informe o Evento que ser gerado";
        }

        /// <summary>
        /// Executa o processamento 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button2_Click(object sender, EventArgs e)
        {
            FileInfo[] arquivos = new FileInfo[0];
            DirectoryInfo directory;
            string dicetorioDoResultado = "";

            if (string.IsNullOrEmpty(textEvento.Text))
            {
                MessageBox.Show("Evento não selecionao");
                return;
            }


            if (string.IsNullOrEmpty(folderDialog.SelectedPath))
            {
                MessageBox.Show("Não há arquivos para processar");
                return;
            }


            textBox.Text = $"Processando : {folderDialog.SelectedPath} \r\n";

            try
            {
                directory = new DirectoryInfo(folderDialog.SelectedPath);
                arquivos = directory.GetFiles();

                this.progressBar1.Maximum = arquivos.Count();

                dicetorioDoResultado = CriaDiretorioResultado();
            }
            catch (Exception ex)
            {

                MessageBox.Show($"Erro ao ler a Pasta {folderDialog.SelectedPath} {ex.Message}");
            }

            using (SqlConnection connection = new SqlConnection(Configuracao.ConnectionString))
            {
                //Abre a connecxão com o banco de dados 
                connection.Open();
                ProcessarArquivos(arquivos, dicetorioDoResultado, connection);
                progressBar1.Value = 0;

                MessageBox.Show("Processo finalizado! ", "Finalizado", MessageBoxButtons.OK, MessageBoxIcon.Information);

                AbreDiretorio(dicetorioDoResultado);
            }
        }

        private void ProcessarArquivos(FileInfo[] arquivos, string dicetorioDoResultado, SqlConnection connection)
        {
            foreach (var arquivo in arquivos)
            {


                XmlDocument xml = new XmlDocument();
                try
                {
                    xml.Load(arquivo.FullName);
                }
                catch (XmlException ex)
                {
                    MessageBox.Show($"Erro ao abrir o arquivo {arquivo.FullName} {ex.Message} {ex.InnerException}");
                    continue;
                }


                var cpfNode = xml.GetElementsByTagName("cpfTrab");
                if (cpfNode == null)
                {
                    MessageBox.Show($"Tag cpfTrab não presente no arquivo {arquivo.FullName}");
                    continue;
                }

                XmlNodeList idNode = null;

                if (textEvento.Text == "s-2210")
                {
                    idNode = xml.GetElementsByTagName("evtCAT");
                }
                if (textEvento.Text == "s-2220")
                {
                    idNode = xml.GetElementsByTagName("evtMonit");
                }
                if (textEvento.Text == "s-2240")
                {
                    idNode = xml.GetElementsByTagName("evtExpRisco");
                }
                if (idNode == null)
                {
                    MessageBox.Show($"Tag evtCAT, evtExpRisco ou evtMonit não presente no arquivo {arquivo.FullName}");
                    continue;
                }

                string cpf = cpfNode[0].InnerText.PadLeft(11, '0');

                string Id = idNode[0]
                        .Attributes["Id"].Value
                        .Substring(idNode[0].Attributes["Id"].Value.Length - 11, 11);



                var filial = connection.Query<string>(@$"SELECT TOP 1 RA_FILIAL
                                                                FROM {Configuracao.TabelaSRA} WITH(NOLOCK) 
                                                                WHERE RA_CIC='{cpf}' AND D_E_L_E_T_='' ")
                                                     .FirstOrDefault();

                if (string.IsNullOrEmpty(filial))
                {
                    textBox.Text += $"CPF {cpf} do arquivo {arquivo.FullName} não encontrado no Protheus \n";
                    continue;
                }

                var matricula = connection.Query<string>($@"SELECT top 1  C9V_MATRIC FROM {Configuracao.TabelaC9V}
                                                                    WHERE C9V_CPF='{cpf}'
                                                                    AND D_E_L_E_T_=''")
                                                        .FirstOrDefault();



                if (string.IsNullOrEmpty(matricula))
                {
                    textBox.Text += $"Matrícula do E-Social do arquivo {arquivo.FullName} não encontrado no Protheus. CPF: {cpf} \r\n";
                    continue;
                }

                var ideVinculo = xml.GetElementsByTagName("ideVinculo");

                string empresa = "01";
                string evento = textEvento.Text;
                string data = DateTime.Now.ToString("yyyyMMdd");

                /* Layout
                 01 – Empresa acrescido do separador underline (_)
                 1001 – Filial da empresa acrescido do separador underline (_)
                 s-2220 – Evento do e-Social acrescido do separador underline (_)
                 20210430 – Data no formato AAAAMMDD acrescido do separador underline (_)
                 154353 – ID do arquivo encontrado internamente na tag <evtMonit> acrescido de um sequencial.

                 */

                string newFileName = String.Concat(empresa, "_", filial, "_", evento, "_", data, "_", Id);
                var arquivoDestino = dicetorioDoResultado + newFileName + ".XML";
                xml.Save(arquivoDestino);
                //arquivo.CopyTo(string.Concat(dicetorioDoResultado, newFileName, ".XML"), true);
                progressBar1.Value++;

            }
        }

        private string CriaDiretorioResultado()
        {
            var dicetorioDoResultado = String.Concat(folderDialog.SelectedPath, "\\Processado\\" + DateTime.Now.Year.ToString() + "\\" + DateTime.Now.Month.ToString() + "\\");
            if (!Directory.Exists(dicetorioDoResultado))
            {
                Directory.CreateDirectory(dicetorioDoResultado);
            }

            return dicetorioDoResultado;
        }

        private static void AbreDiretorio(string dicetorioDoResultado)
        {
            ProcessStartInfo startInfo = new ProcessStartInfo()
            {
                Arguments = dicetorioDoResultado,
                FileName = "explorer.exe"
            };
            Process.Start(startInfo);
        }

        private void sobreToolStripMenuItem_Click(object sender, EventArgs e)
        {
            new SobreForm().Show();
        }

        private void button1_Click_1(object sender, EventArgs e)
        {
            this.Close();
        }
    }
}
