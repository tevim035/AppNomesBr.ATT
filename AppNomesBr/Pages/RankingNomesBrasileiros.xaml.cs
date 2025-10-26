using AppNomesBr.Domain.DataTransferObject.ExternalIntegrations.IBGE.Censos;
using AppNomesBr.Domain.Interfaces.Services;
using System.Text.Json;

namespace AppNomesBr.Pages;

public partial class RankingNomesBrasileiros : ContentPage
{
    private readonly INomesBrService service;

    public RankingNomesBrasileiros(INomesBrService service)
    {
        InitializeComponent();
        this.service = service;
    }

    protected override async void OnAppearing()
    {
        await CarregarNomes();
        base.OnAppearing();
    }

    private async Task CarregarNomes(string? cidade = null, string? sexo = null, string? estado = null)
    {
        var result = await service.ListaTop20Nacional(cidade, sexo, estado);
        this.GrdNomesBr.ItemsSource = result.FirstOrDefault()?.Resultado;
    }

    private async void BtnAtualizar_Clicked(object sender, EventArgs e)
    {
        string cidade = CidadeEntry.Text?.Trim();
        string sexo = SexoPicker.SelectedItem?.ToString();
        string estado = EstadoPicker.SelectedItem?.ToString();

        await CarregarNomes(cidade, sexo, estado);
    }

}
