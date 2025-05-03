using Domain;
using Translations;

// Basic builders for all properties
var exercise = new ExerciseBuilder()
    .WithName("How to make a plan come together?")
    .WithDescription("Secret")
    .WithDifficulty(5)
    .Build();

var groupBuilder = new GroupBuilder()
    .WithName("The A-Team")
    .WithUsers([new UserBuilder().WithName("B.A. Baracus").WithEmail("shut_up@fool.com").Build()]);

var user = new UserRawBuilder()
    .WithName("John 'Hannibal' Smith")
    .WithEmail("whatever@email.com")
    .WithExercises([exercise])
    .Build();

// Supports collection modifications
groupBuilder.ClearUsers();
groupBuilder.AddUsersItem(user);

var group = groupBuilder.Build();

// Translations generated from json files
var greeting = translations_en.greeting;
var title = translations_nl.title;
var description = translations_fr.description;
var save = translations_de.save;
Console.WriteLine($"{greeting} {title} {description} {save}");