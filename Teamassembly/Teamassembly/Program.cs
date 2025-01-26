using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Newtonsoft.Json;

public class Professional
{
    public string Name { get; set; }
    public string Title { get; set; }
    public List<string> Skills { get; set; }
    public double CostPerHour { get; set; }
    public double Rating { get; set; }
}

public class OpenAIResponse
{
    public List<Choice> Choices { get; set; }
}

public class Choice
{
    public string Text { get; set; }
}

public class Program
{
    private static readonly HttpClient client = new HttpClient();

    public static async Task<List<Professional>> LoadProfessionalsAsync(string url)
    {
        var response = await client.GetStringAsync(url);
        return JsonConvert.DeserializeObject<List<Professional>>(response);
    }

    public static List<Professional> AssembleTeam(List<string> skillsNeeded, double budget, List<Professional> professionals)
    {
        List<Professional> team = new List<Professional>();
        var selectedSkills = new HashSet<string>(skillsNeeded);

        // Include a project manager
        var projectManagers = professionals.Where(member => member.Title.Contains("Project Manager") && member.CostPerHour <= budget).ToList();
        if (projectManagers.Any())
        {
            projectManagers = projectManagers.OrderByDescending(x => x.Rating).ThenBy(x => x.CostPerHour).ToList();
            var selectedManager = projectManagers.First();
            team.Add(selectedManager);
            budget -= selectedManager.CostPerHour;
        }
        else
        {
            return new List<Professional>(); // Return an empty list if no project manager can be selected
        }

        foreach (var skill in selectedSkills)
        {
            var skillMembers = professionals.Where(member => member.Skills.Contains(skill) && member.CostPerHour <= budget).ToList();
            if (!skillMembers.Any()) continue;

            // Sort by rating (descending) and cost (ascending)
            skillMembers = skillMembers.OrderByDescending(x => x.Rating).ThenBy(x => x.CostPerHour).ToList();
            var selectedMember = skillMembers.First(); // Choose the highest rated and lowest cost
            team.Add(selectedMember);
            budget -= selectedMember.CostPerHour;
        }

        return team;
    }

    public static async Task<string> GenerateSkillsAsync(string issue)
    {
        var apiKey = "sk-proj-0FKCjNz4PL1kvNfG2JzaT3BlbkFJHbGsEgkyfzGvwtKG0VQC";
        var prompt = $"Generate a concise list of high-level skills needed to address the following issue: {issue}. Only include essential skills and separate them by commas.";

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
        var requestContent = new
        {
            model = "gpt-3.5-turbo-instruct",
            prompt = prompt,
            temperature = 0.5,
            max_tokens = 100
        };
        var response = await client.PostAsJsonAsync("https://api.openai.com/v1/completions", requestContent);

        response.EnsureSuccessStatusCode();
        var responseBody = await response.Content.ReadAsStringAsync();
        var result = JsonConvert.DeserializeObject<OpenAIResponse>(responseBody);
        var skills = result.Choices[0].Text.Trim();
        return skills;
    }

    public static async Task Main(string[] args)
    {
        Console.Write("Please describe the issue you're facing and need assistance with: ");
        var issue = Console.ReadLine();

        var skills = await GenerateSkillsAsync(issue);
        Console.WriteLine("Recommended Skills:");
        Console.WriteLine(skills);

        var skillsNeeded = skills.Split(',').Select(s => s.Trim()).ToList();
        Console.Write("Please enter your budget: $");
        var budget = Convert.ToDouble(Console.ReadLine());

        var professionals = await LoadProfessionalsAsync("https://your-endpoint-url.com/api/professionals");

        var team = AssembleTeam(skillsNeeded, budget, professionals);

        if (team.Any())
        {
            Console.WriteLine("Assembled Team:");
            foreach (var member in team)
            {
                Console.WriteLine($"Name: {member.Name}, Job Title: {member.Title}, Skills: {string.Join(", ", member.Skills)}, Cost: ${member.CostPerHour:F2}, Rating: {member.Rating:F2}");
            }
        }
        else
        {
            Console.WriteLine("Unable to assemble a team with the provided skills and budget.");
        }
    }
}
