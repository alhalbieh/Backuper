using DataDownload.Models;

namespace DataDownload;

public class Downloader
{
    private readonly HttpClient _httpClient;

    public Downloader(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task Download(string downloadFolder, List<string> names, List<string> urls)
    {
        var existingNames = Directory.EnumerateFiles(downloadFolder).ToList();
        if (existingNames.Count > 0)
        {
            existingNames.ForEach(n => n = n.Split(@"\")[^1]);
            var allNames = existingNames.Concat(names).ToList();
            names = allNames.RenameDuplicates().GetRange(existingNames.Count, names.Count);
        }
        else
            names = names.RenameDuplicates();

        var responseTasks = urls.Select(u => _httpClient.GetAsync(u));
        var responses = await Task.WhenAll(responseTasks);
        var streamTasks = responses.Select(r => r.Content.ReadAsStreamAsync());
        var streams = await Task.WhenAll(streamTasks);

        var fullNewNames = names.Select(n => Path.Combine(downloadFolder, n));
        var fileStreams = fullNewNames.Select(n => File.Create(n));
        var downloadTasks = streams.Zip(fileStreams, (s, fs) => s.CopyToAsync(fs));
        await Task.WhenAll(downloadTasks);
    }

    public async Task Download(string downloadFolder, List<Pdf> pdfs)
    {
        var existingNames = Directory.EnumerateFiles(downloadFolder).ToList();
        var names = pdfs.Select(p => p.Name).ToList();
        var newNames = new List<string>();

        if (existingNames.Count > 0)
        {
            existingNames.ForEach(name => name = name.Split(@"\")[^1]);
            var allNames = existingNames.Concat(names);
            newNames = allNames.RenameDuplicates().GetRange(existingNames.Count, names.Count);
        }
        else
        {
            newNames = names.RenameDuplicates();
        }

        foreach (var (pdf, newName) in pdfs.Zip(newNames))
            pdf.Name = newName;

        var responseTasks = pdfs.Select(p => _httpClient.GetAsync(p.Url));
        var responses = await Task.WhenAll(responseTasks);
        var streamTasks = responses.Select(r => r.Content.ReadAsStreamAsync());
        var streams = await Task.WhenAll(streamTasks);

        var fullNewNames = pdfs.Select(p => Path.Combine(downloadFolder, p.Name));
        var fileStreams = fullNewNames.Select(n => File.Create(n));
        var downloadTasks = streams.Zip(fileStreams, (s, fs) => s.CopyToAsync(fs));
        await Task.WhenAll(downloadTasks);
    }
}
