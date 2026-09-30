using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text.Json;
using GZCTF.Models.Data;
using GZCTF.Models.Request.Game;
using GZCTF.Utils;
using Xunit;

namespace GZCTF.Test.UnitTests.Models;

public class ChoiceExamTests
{
    private static ChoiceExamConfigModel Config() => new()
    {
        Enabled = true, SingleCount = 1, MultipleCount = 1, SingleScore = 5, MultipleScore = 10,
        Questions =
        [
            new() { Type = ChoiceQuestionType.Single, Content = "Single", Options = ["A", "B"], CorrectAnswers = [0] },
            new() { Type = ChoiceQuestionType.Multiple, Content = "Multiple", Options = ["A", "B", "C"], CorrectAnswers = [0, 2] }
        ]
    };

    private static ChoiceAttempt Attempt() => ChoiceExamHelper.CreateAttempt(1, 2, Config());

    private static void Save(ChoiceAttempt attempt, int question, params int[] options) =>
        Assert.Null(ChoiceExamHelper.SaveAnswer(attempt, question,
            new ChoiceAnswerModel { Version = attempt.Version, SelectedOptions = options }));

    [Fact]
    public void DraftRestoresWithoutExposingAnswerKeysOrScores()
    {
        var attempt = Attempt();
        Save(attempt, 1, 1);
        var restored = JsonSerializer.Deserialize<ChoiceAttempt>(JsonSerializer.Serialize(attempt))!;
        var model = ChoiceExamHelper.ToModel(restored);
        Assert.Equal(new[] { 1 }, model.Answers[1]);
        Assert.Equal(attempt.QuestionsJson, restored.QuestionsJson);
        Assert.Null(model.Score);
        Assert.DoesNotContain("correctAnswers", JsonSerializer.Serialize(model, ChoiceExamHelper.JsonOptions));
        Assert.All(model.Questions, q => Assert.IsType<ChoicePaperQuestion>(q));
    }

    [Fact]
    public void CannotSubmitIncompletePaper()
    {
        var attempt = Attempt();
        Save(attempt, 1, 0);
        Assert.NotNull(ChoiceExamHelper.Submit(attempt, attempt.Version));
        Assert.Null(attempt.SubmittedAt);
        Assert.Null(attempt.Score);
    }

    [Theory]
    [InlineData(new[] { 2, 0 }, 15)]
    [InlineData(new[] { 0 }, 5)]
    [InlineData(new[] { 0, 1, 2 }, 5)]
    [InlineData(new[] { 1 }, 5)]
    public void MultipleChoiceRequiresExactSet(int[] multiple, int expectedScore)
    {
        var attempt = Attempt();
        Save(attempt, 1, 0);
        Save(attempt, 2, multiple);
        Assert.Null(ChoiceExamHelper.Submit(attempt, attempt.Version));
        Assert.Equal(expectedScore, attempt.Score);
        Assert.NotNull(attempt.SubmittedAt);
    }

    [Fact]
    public void FinalSubmissionIsIdempotentAndCannotBeEdited()
    {
        var attempt = Attempt();
        Save(attempt, 1, 0);
        Save(attempt, 2, 0, 2);
        Assert.Null(ChoiceExamHelper.Submit(attempt, attempt.Version));
        var before = JsonSerializer.Serialize(attempt);
        Assert.Null(ChoiceExamHelper.Submit(attempt, 1));
        Assert.NotNull(ChoiceExamHelper.SaveAnswer(attempt, 1,
            new ChoiceAnswerModel { Version = attempt.Version, SelectedOptions = [1] }));
        Assert.Equal(before, JsonSerializer.Serialize(attempt));
    }

    [Fact]
    public void StaleSaveAndSubmitCannotOverwriteNewerDraft()
    {
        var attempt = Attempt();
        var oldVersion = attempt.Version;
        Save(attempt, 1, 1);
        Save(attempt, 2, 0, 2);
        var before = JsonSerializer.Serialize(attempt);
        Assert.NotNull(ChoiceExamHelper.SaveAnswer(attempt, 1,
            new ChoiceAnswerModel { Version = oldVersion, SelectedOptions = [0] }));
        Assert.NotNull(ChoiceExamHelper.Submit(attempt, oldVersion));
        Assert.Equal(before, JsonSerializer.Serialize(attempt));
    }

    [Theory]
    [InlineData(1, new[] { 0, 1 })]
    [InlineData(1, new[] { -1 })]
    [InlineData(2, new[] { 3 })]
    [InlineData(2, new[] { 0, 0 })]
    [InlineData(99, new[] { 0 })]
    public void InvalidOptionsDoNotChangeTheDraft(int questionId, int[] options)
    {
        var attempt = Attempt();
        var before = JsonSerializer.Serialize(attempt);
        Assert.NotNull(ChoiceExamHelper.SaveAnswer(attempt, questionId,
            new ChoiceAnswerModel { Version = attempt.Version, SelectedOptions = options }));
        Assert.Equal(before, JsonSerializer.Serialize(attempt));
    }

    [Fact]
    public void ClearingAnAnswerMakesPaperIncomplete()
    {
        var attempt = Attempt();
        Save(attempt, 1, 0);
        Save(attempt, 2, 0, 2);
        Save(attempt, 2);
        Assert.False(ChoiceExamHelper.ReadAnswers(attempt).ContainsKey(2));
        Assert.NotNull(ChoiceExamHelper.Submit(attempt, attempt.Version));
    }

    [Fact]
    public void ConfigRejectsInsufficientBankAndInvalidAnswerKeys()
    {
        var config = Config();
        config.SingleCount = 2;
        Assert.NotEmpty(config.Validate(new ValidationContext(config)));
        config.SingleCount = 1;
        config.Questions[0].CorrectAnswers = [0, 1];
        Assert.NotEmpty(config.Validate(new ValidationContext(config)));
        config.Questions[0].CorrectAnswers = [0];
        config.Questions[1].CorrectAnswers = [2];
        Assert.NotEmpty(config.Validate(new ValidationContext(config)));
    }

    [Fact]
    public void SamplingUsesConfiguredCountsAndFreezesScores()
    {
        var config = Config();
        config.Questions = [.. config.Questions, new() { Type = ChoiceQuestionType.Single,
            Content = "Extra", Options = ["yes", "no"], CorrectAnswers = [1] }];
        var attempt = ChoiceExamHelper.CreateAttempt(7, 8, config);
        config.SingleScore = 999;
        var questions = ChoiceExamHelper.ReadQuestions(attempt);
        Assert.Equal(2, questions.Length);
        Assert.Single(questions, q => q.Type == ChoiceQuestionType.Single);
        Assert.Equal(5, questions.Single(q => q.Type == ChoiceQuestionType.Single).Score);
        Assert.Equal(new[] { 1, 2 }, questions.Select(q => q.Id));
        Assert.Equal(7, attempt.ParticipationId);
    }
}
