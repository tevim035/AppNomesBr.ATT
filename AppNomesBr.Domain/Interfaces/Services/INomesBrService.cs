using AppNomesBr.Domain.DataTransferObject.ExternalIntegrations.IBGE.Censos;

namespace AppNomesBr.Domain.Interfaces.Services
{
    public interface INomesBrService
    {
        Task<RankingNomesRoot[]> ListaTop20Nacional(string? cidade = null, string? sexo = null, string? estado = null);
        Task<RankingNomesRoot[]> ListaMeuRanking();
        Task InserirNovoRegistroNoRanking(string nome, string sexo = "M");
    }
}
