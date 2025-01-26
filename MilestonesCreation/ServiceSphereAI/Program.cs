using System;
using System.Collections.Generic;
using System.Xml;
using Newtonsoft.Json;
using RestSharp;

class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("Please describe the project or issue you need assistance with:");
        string userInput = Console.ReadLine();

        Console.WriteLine("Please enter the total budget for the project:");
        string budgetInput = Console.ReadLine();

        if (!float.TryParse(budgetInput, out float budget))
        {
            Console.WriteLine("Invalid budget input. Please enter a numerical value.");
            return;
        }

        string mainMilestones = GenerateMainMilestones(userInput, budget);
        List<Milestone> milestonesList = ParseAndFormatMilestones(mainMilestones, budget);
        string milestonesJson = JsonConvert.SerializeObject(milestonesList, Formatting.Indented);

        Console.WriteLine("Main milestones in JSON format:");
        Console.WriteLine(milestonesJson);
    }

    static string GenerateMainMilestones(string userInput, float budget)
    {
        var client = new RestClient("https://api.openai.com/v1/chat/completions");
        var request = new RestRequest(Method.POST);
        request.AddHeader("Authorization", "Bearer sk-proj-0FKCjNz4PL1kvNfG2JzaT3BlbkFJHbGsEgkyfzGvwtKG0VQC");
        request.AddHeader("Content-Type", "application/json");

        var body = new
        {
            model = "gpt-3.5-turbo",
            messages = new[]
            {
                new { role = "system", content = "You are a project management assistant." },
                new { role = "user", content = $"Generate main milestones and their related tasks, along with the timeline in the format of 'week number : start day number to end day number : task details' for the given project. Ensure that tasks that can be done concurrently are included in the same timeline. Exclude hiring and contracting tasks. Also, divide the budget of ${budget} among the milestones and provide an estimate for each. \n- {userInput}" }
            },
            temperature = 0.5,
            max_tokens = 500
        };

        request.AddJsonBody(body);

        IRestResponse response = client.Execute(request);
        var content = JsonConvert.DeserializeObject<dynamic>(response.Content);

        return content.choices[0].message["content"].ToString().Trim();
    }

    static List<Milestone> ParseAndFormatMilestones(string mainMilestones, float budget)
    {
        string[] lines = mainMilestones.Split('\n');
        var milestonesList = new List<Milestone>();
        int numMilestones = 0;

        foreach (var line in lines)
        {
            if (line.Contains(":") && line.Contains("-"))
                numMilestones++;
        }

        foreach (var line in lines)
        {
            if (line.Contains(":") && line.Contains("-"))
            {
                var parts = line.Split(new[] { ':' }, 3);
                if (parts.Length == 3)
                {
                    string week = parts[0].Trim();
                    string days = parts[1].Trim();
                    string task = parts[2].Trim();
                    float budgetEstimate = (float)Math.Round(budget / numMilestones, 2);

                    var milestone = new Milestone
                    {
                        MilestoneDescription = task,
                        Budget = budgetEstimate,
                        Duration = $"{week} {days}"
                    };
                    milestonesList.Add(milestone);
                }
            }
        }

        return milestonesList;
    }
}

class Milestone
{
    public string MilestoneDescription { get; set; }
    public float Budget { get; set; }
    public string Duration { get; set; }
}
