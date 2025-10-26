namespace AppNomesBr.Domain.Interfaces.ExternalIntegrations.IBGE.Censos
{
    public interface INomesApi
    {
        Task<string> RetornaCensosNomesRanking();
        Task<string> RetornaCensosNomesPeriodo(string nome);

        // Novo método
        Task<string> RetornaCensosNomesRanking(string? cidade, string? sexo);
    }
}
