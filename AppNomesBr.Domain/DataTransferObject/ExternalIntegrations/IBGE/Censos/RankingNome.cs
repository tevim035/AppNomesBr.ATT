﻿using System.Text.Json.Serialization;

namespace AppNomesBr.Domain.DataTransferObject.ExternalIntegrations.IBGE.Censos
{
    public class RankingNome
    {
        [JsonPropertyName("nome")]
        public string Nome { get; set; } = string.Empty;

        [JsonPropertyName("frequencia")]
        public long Frequencia { get; set; }

        [JsonPropertyName("ranking")]
        public int Ranking { get; set; }

        // Campo adicionado para armazenar o sexo do nome
        [JsonPropertyName("sexo")]
        public string Sexo { get; set; } = string.Empty;
    }
}
