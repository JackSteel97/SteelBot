using System;
using System.Collections.Generic;
using System.Linq;

namespace SteelBot.DiscordModules.Puzzle.Questions;

public class QuestionFactory
{
    private readonly Dictionary<int, IQuestion> _questions = new();

    public QuestionFactory()
    {
        Setup();
    }

    public IQuestion GetQuestion(int number)
    {
        if (!_questions.TryGetValue(number, out var question)) throw new NotSupportedException($"There is no supported implementation for question {number}");

        return question;
    }

    private void Setup()
    {
        var handlers = AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(s => s.GetTypes())
            .Where(p => typeof(IQuestion).IsAssignableFrom(p) && p.IsClass);

        foreach (var handler in handlers)
        {
            var questionInstance = (IQuestion)Activator.CreateInstance(handler);
            _questions.Add(questionInstance.GetPuzzleNumber(), questionInstance);
        }
    }
}