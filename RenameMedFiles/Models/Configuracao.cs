using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RenameMedFiles.Models
{
    internal class Configuracao
    {
        public string ConnectionString { get; set; }
        public string TabelaSRA { get; set; }
        public string TabelaC9V { get; set; }
        public string Token { get; set; }

        public bool IsValidToken
        {
            get
            {

                if (string.IsNullOrEmpty(Token))
                    return false;


                var arrayOfChars = Token.ToCharArray();
                float total = 0;
                float resto;
                float ultimoDigito;


                for (int i = 0; i < arrayOfChars.Length-1; i++)
                {
                    float numero;
                    var isNumeric = float.TryParse(arrayOfChars[i].ToString(), out numero);
                    if (!isNumeric)
                        return false;

                    total += numero;
                }
                resto = total % 11;
                float digitoVerificador = 10 - resto;
                if (digitoVerificador == 0)
                {
                    digitoVerificador = 1;
                }
                float.TryParse(arrayOfChars.Last().ToString(), out ultimoDigito);


                if (digitoVerificador == ultimoDigito)
                    return true;
                else
                    return false;
            }
        }
    }
}
