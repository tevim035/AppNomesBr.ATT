using AppNomesBr.Domain.DataTransferObject.ExternalIntegrations.IBGE.Censos;
using AppNomesBr.Domain.Interfaces.ExternalIntegrations.IBGE.Censos;
using AppNomesBr.Domain.Interfaces.Services;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using AppNomesBr.Domain.Interfaces.Repositories;
using AppNomesBr.Domain.Entities;
using System.Text;

namespace AppNomesBr.Service
{
    public class NomesBrService : INomesBrService
    {
        private readonly INomesApi ibgeNomesApiService;
        private readonly ILogger<NomesBrService> logger;
        private readonly INomesBrRepository nomesBrRepository;
        public NomesBrService(INomesApi ibgeNomesApiService, ILogger<NomesBrService> logger, INomesBrRepository nomesBrRepository)
        {
            this.ibgeNomesApiService = ibgeNomesApiService;
            this.logger = logger;
            this.nomesBrRepository = nomesBrRepository;
        }

        public async Task<RankingNomesRoot[]> ListaTop20Nacional(string? cidade = null, string? sexo = null, string? estado = null)
        {
            try
            {
                logger.LogInformation("Consultando top 20 nomes. Cidade: {Cidade}, Estado: {Estado}, Sexo: {Sexo}", cidade, estado, sexo);

                string result;

                // Se nenhum filtro for informado, consulta o ranking nacional
                if (string.IsNullOrWhiteSpace(cidade) && string.IsNullOrWhiteSpace(sexo))
                {
                    result = await ibgeNomesApiService.RetornaCensosNomesRanking();
                }
                else
                {
                    string? codigoMunicipio = null;

                    // Se cidade e estado foram informados, tenta buscar o código IBGE
                    if (!string.IsNullOrWhiteSpace(cidade) && !string.IsNullOrWhiteSpace(estado))
                    {
                        codigoMunicipio = await ObterCodigoMunicipioPorNomeAsync(cidade, estado);
                        logger.LogInformation("Código IBGE encontrado: {Codigo}", codigoMunicipio);
                    }

                    result = await ibgeNomesApiService.RetornaCensosNomesRanking(codigoMunicipio ?? cidade, sexo);
                }

                var rankingNomesRoot = JsonSerializer.Deserialize<RankingNomesRoot[]>(result);
                if (rankingNomesRoot == null)
                    throw new InvalidDataException("Variável 'rankingNomesRoot' é nula!");

                return rankingNomesRoot;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "[ERRO]: {Message}", ex.Message);
                return [];
            }
        }

        /// <summary>
        /// Busca o código IBGE de um município a partir do nome e UF.
        /// </summary>
        private static async Task<string?> ObterCodigoMunicipioPorNomeAsync(string nomeCidade, string uf)
        {
            try
            {
                using HttpClient http = new();
                string url = "https://servicodados.ibge.gov.br/api/v1/localidades/municipios";
                var response = await http.GetStringAsync(url);

                using var jsonDoc = JsonDocument.Parse(response);
                var municipios = jsonDoc.RootElement.EnumerateArray();

                foreach (var municipio in municipios)
                {
                    string nome = municipio.GetProperty("nome").GetString() ?? "";
                    string siglaUF = municipio.GetProperty("microrregiao")
                                             .GetProperty("mesorregiao")
                                             .GetProperty("UF")
                                             .GetProperty("sigla")
                                             .GetString() ?? "";

                    if (string.Equals(nome, nomeCidade, StringComparison.OrdinalIgnoreCase) &&
                        string.Equals(siglaUF, uf, StringComparison.OrdinalIgnoreCase))
                    {
                        return municipio.GetProperty("id").GetRawText(); // ID é o código IBGE
                    }
                }

                return null;
            }
            catch
            {
                return null;
            }
        }



        public async Task InserirNovoRegistroNoRanking(string nome, string sexo = "M")
        {
            try
            {
                logger.LogInformation("Inserir novo registro no ranking. Nome: {Nome}, Sexo: {Sexo}", nome, sexo);

                var result = await ibgeNomesApiService.RetornaCensosNomesPeriodo(nome);
                var frequenciaPeriodo = JsonSerializer.Deserialize<NomeFrequenciaPeriodoRoot[]>(result)
                    ?? throw new InvalidDataException("Erro ao buscar pelos dados do nome informado");

                var resultado = frequenciaPeriodo.FirstOrDefault()?.Resultado
                    ?? throw new InvalidDataException("Erro ao buscar pelos dados do nome informado");

                var novoRegistro = new NomesBr
                {
                    Nome = nome,
                    Periodo = FormataPeriodo(resultado),
                    Ranking = 1,
                    Frequencia = resultado.Sum(x => x.Frequencia),
                    Sexo = sexo.ToUpper() // Adiciona o sexo
                };

                List<NomesBr> antigos = await nomesBrRepository.GetAll();
                antigos.Add(novoRegistro);
                await AtualizarRanking(antigos);

                novoRegistro.Ranking = antigos[^1].Ranking;

                await nomesBrRepository.Create(novoRegistro);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "[ERRO]: {Message}", ex.Message);
            }
        }


        public async Task<RankingNomesRoot[]> ListaMeuRanking()
        {
            var consultaTodos = await nomesBrRepository.GetAll();

            ArgumentNullException.ThrowIfNull(consultaTodos);

            var retorno = new List<RankingNomesRoot>();
            retorno.Add(new RankingNomesRoot { Resultado = new List<RankingNome>() });

            for (int i = 0; i < consultaTodos.Count; i++)
            {
                var novo = new RankingNome
                {
                    Frequencia = consultaTodos[i].Frequencia,
                    Nome = consultaTodos[i].Nome,
                    Ranking = consultaTodos[i].Ranking,
                    Sexo = consultaTodos[i].Sexo // agora incluímos o sexo
                };

                retorno[0].Resultado?.Add(novo);
            }

            retorno[0].Resultado = retorno[0].Resultado?.OrderBy(x => x.Ranking).ToList();

            return retorno.ToArray();
        }

        private static string FormataPeriodo(List<FrequenciaPeriodo>? periodo)
        {
            ArgumentNullException.ThrowIfNull(periodo);

            string primeiroPeriodo = periodo[0].Periodo;
            string? UltimoPeriodo = periodo[^1].Periodo;

            if(primeiroPeriodo != UltimoPeriodo)
            {
                StringBuilder sb = new();
                if (primeiroPeriodo?[..1] == "[")
                {
                    sb.Append('[');
                    primeiroPeriodo = primeiroPeriodo.Substring(1,4);
                    sb.Append(primeiroPeriodo);
                    sb.Append(" - ");
                    string? temp = UltimoPeriodo?.Replace("[", "]");
                    sb.Append(temp?[(temp.IndexOf(',') + 1)..]);
                }
                else
                {
                    sb.Append('[');
                    sb.Append(primeiroPeriodo?.Replace("[", " - "));
                    string? temp = UltimoPeriodo?.Replace("[", "]");
                    sb.Append(temp?[(temp.IndexOf(',') + 1)..]);
                }

                return sb.ToString();
            }

            return primeiroPeriodo;
        }

        private static List<NomesBr> OrganizarRanking(List<NomesBr> nomes)
        {
            var ordenados = nomes.OrderByDescending(x => x.Frequencia).ToList();
            for (int i = 0; i < ordenados.Count; i++)
                ordenados[i].Ranking = i + 1;

            return ordenados;
        }

        private async Task AtualizarRanking(List<NomesBr> nomes)
        {
            nomes = OrganizarRanking(nomes);
            for (int i = 0; i < nomes.Count; i++)
                await nomesBrRepository.Update(nomes[i]);
            
        }
    }
}
