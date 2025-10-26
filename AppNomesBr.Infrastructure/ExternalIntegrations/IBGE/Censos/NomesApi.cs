using AppNomesBr.Domain.Interfaces.ExternalIntegrations.IBGE.Censos;
using System.Net.Http;
using System.Web;

namespace AppNomesBr.Infrastructure.ExternalIntegrations.IBGE.Censos
{
    public class NomesApi : INomesApi
    {
        private readonly string baseUrl = "https://servicodados.ibge.gov.br/api/v2/censos/nomes/";
        private readonly string rankingEndpoint = "ranking";
        private readonly HttpClient httpClient;

        public NomesApi(HttpClient httpClient)
        {
            this.httpClient = httpClient;
        }

        public async Task<string> RetornaCensosNomesRanking()
        {
            var response = await httpClient.GetAsync(baseUrl + rankingEndpoint);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsStringAsync();
        }

        public async Task<string> RetornaCensosNomesRanking(string? cidade, string? sexo)
        {
            // Monta a query string com base nos parâmetros
            var builder = new UriBuilder(baseUrl + rankingEndpoint);
            var query = HttpUtility.ParseQueryString(string.Empty);

            if (!string.IsNullOrWhiteSpace(cidade))
                query["localidade"] = cidade; // Pode ser código IBGE ou nome, conforme sua lógica

            if (!string.IsNullOrWhiteSpace(sexo))
                query["sexo"] = sexo.StartsWith("M", StringComparison.OrdinalIgnoreCase) ? "M" : "F";

            builder.Query = query.ToString();

            var finalUrl = builder.ToString();

            var response = await httpClient.GetAsync(finalUrl);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsStringAsync();
        }

        public async Task<string> RetornaCensosNomesPeriodo(string nome)
        {
            var url = baseUrl + nome;
            var response = await httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsStringAsync();
        }
    }
}
