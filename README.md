# Utils
## RenameMedFiles

Renomeia arquivos XML de evnetos do e-social (2210,2220 e 2240) para o padrão de importação do TAF Protheus.
Facilita renomenando os arquivos em massa, gerando uma cópia dos arquivos originias. 
Como é necessário a filial do funcionário para correta renomeação, o sistema faz o acesso a base de dados do Protheus, por connection string, retorna a correta filial do sistema com base no CPF do funcionário. 

### Layout do arquivo gerado 

 01 – Empresa acrescido do separador underline (_)
 1001 – Filial da empresa acrescido do separador underline (_)
 s-2220 – Evento do e-Social acrescido do separador underline (_)
 20210430 – Data no formato AAAAMMDD acrescido do separador underline (_)
 154353 – ID do arquivo encontrado internamente na tag <evtMonit> acrescido de um sequencial.

Exemplo: `01_1001_s-2220_20210430_154353.xml`