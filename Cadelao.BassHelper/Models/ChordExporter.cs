using System.Globalization;
using System.Text;

namespace Cadelao.BassHelper.Models;

public static class ChordExporter
{
    public static void Save(string fileName, AnalysisResult result)
    {
        string extension = Path.GetExtension(fileName).ToLowerInvariant();
        var sb = new StringBuilder();
        if (extension == ".csv")
        {
            sb.AppendLine("Inicio_s;Fim_s;Duracao_s;Triade;Qualidade;Fundamental;Terca;Quinta;Casas_4_cordas;Motor_origem;Cifra_bruta;Corrigido");
            foreach (ChordSegment seg in result.ChordSegments)
            {
                bool isTriad = ChordVocabulary.TryParse(seg.EffectiveName, out var chord);
                string quality = isTriad ? chord!.Quality : "N/D";
                string root = isTriad ? MusicNotes.Names[chord!.Root] : "";
                string third = isTriad ? MusicNotes.Names[chord!.Third] : "";
                string fifth = isTriad ? MusicNotes.Names[chord!.Fifth] : "";
                string fingering = isTriad ? FourStringBass.Suggest(chord!).Describe(chord) : "";
                sb.AppendLine(string.Join(';',
                    seg.Start.ToString("F3", CultureInfo.InvariantCulture),
                    seg.End.ToString("F3", CultureInfo.InvariantCulture),
                    seg.Duration.ToString("F3", CultureInfo.InvariantCulture),
                    seg.EffectiveName, quality, root, third, fifth,
                    fingering.Replace(';', ','), result.Source, seg.RawName,
                    seg.OverrideChord is null ? "nao" : "sim"));
            }
        }
        else if (extension == ".lrc")
        {
            sb.AppendLine("[ti:Cadelao.BassHelper - Tríades estimadas para baixo de quatro cordas]");
            foreach (ChordSegment seg in result.ChordSegments)
            {
                TimeSpan t = TimeSpan.FromSeconds(seg.Start);
                sb.AppendLine($"[{(int)t.TotalMinutes:00}:{t.Seconds:00}.{t.Milliseconds / 10:00}]{seg.EffectiveName}");
            }
        }
        else
        {
            sb.AppendLine("CADELAO.BASSHELPER - TRÍADES POR FRASE PARA BAIXO DE QUATRO CORDAS");
            sb.AppendLine("Estimativa de uma mixagem completa; revise as mudanças pelo ouvido.");
            sb.AppendLine("Posições são sugestões de estudo, não a transcrição exata do LP.");
            sb.AppendLine();
            foreach (ChordSegment seg in result.ChordSegments)
            {
                bool ok = ChordVocabulary.TryParse(seg.EffectiveName, out var chord);
                sb.AppendLine($"{MusicNotes.FormatDetailed(seg.Start)} - {MusicNotes.FormatDetailed(seg.End)}" +
                    (ok ? $"    {chord!.Name,-5} {chord.Quality,-5}    {chord.Notes}   |  " +
                        FourStringBass.Suggest(chord).Describe(chord) : "    -- (sem triade determinada)") +
                    $"  [Chordino: {seg.RawName}]");
            }
        }
        File.WriteAllText(fileName, sb.ToString(), new UTF8Encoding(true));
    }
}
