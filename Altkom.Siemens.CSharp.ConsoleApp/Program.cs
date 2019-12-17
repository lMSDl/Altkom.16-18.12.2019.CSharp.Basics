﻿using Altkom.Siemens.CSharp.CollectionBasedService;
using Altkom.Siemens.CSharp.ConsoleApp.Extensions;
using Altkom.Siemens.CSharp.ConsoleApp.Models;
using Altkom.Siemens.CSharp.IServices;
using Altkom.Siemens.CSharp.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Altkom.Siemens.CSharp.ConsoleApp
{
    class Program
    {
        static ICrud<Person> Context { get; } = new GenericContext<Person>(new List<Person>() {
                new Instructor("Adam", "Adamski", Genders.Male, 3, "Programming") { InstructorId = 1 },
                new Instructor("Piotr", "Piotrowski", Genders.Male, 2, "Economy") { InstructorId = 2 },
                new Instructor("Michał", "Michalski", Genders.Male, 6, "Not specified") { InstructorId = 3 },
                new Student("Ewa", "Michalska", Genders.Female, 2) { StudentId = 4 },
                new Student("Ewa", "Ewowska", Genders.Female, 1) { StudentId = 5 }
            });

        //* delegat - wskaźnik na funkcje
        delegate void Output(string output);
        static Output TextOutput { get; set; }

        //* Func i Action - predefiniowane generyczne delegaty. Action przyjmuje tylko parametry wejściowe, dla Func określamy dodatkowo typ zwracany.
        static Func<IEnumerable<Person>, IEnumerable<Person>> OrderByFunc;

        static Func<IEnumerable<Person>, IEnumerable<Person>> FilterFunc;


        static void Main(string[] args)
        {
            //* Delegaty mogą wskazywać na kilka funkcje jednocześnie. Wszystkie z nich zostaną wykonane podczas wywołania delegata.
            TextOutput += WriteToConsole;
            //* Przypisanie do delegata metody anonimowej z wykorzystaniem wyrażenia lambda
            //* () => Funckja() - wyrażenie lambda nie przyjmujące parametrów. Zamiast parametrów nawiasy.
            //* parametr => Funckja(parametr) - wyrażenie lambda z jednym parametrem
            //* (parametr1, parametr2, ..) => Funcja(parametr1, parametr2, ...) - wyrażenie lambda z wieloma parametrami. Parametry w nawiasach.
            TextOutput += outputString => Debug.WriteLine(outputString);

            do
            {
                DisplayPeople();
            } while (ExecuteCommand(Console.ReadLine()));

        }
        
        static bool ExecuteCommand(string input)
        {
            var splittedInput = input.Split(' ', '.');
            int? id = null;
            if (splittedInput.Length > 1)
            //    int.TryParse(splittedInput[1], out id);
                id = splittedInput[1].ToInt();

            //if (Enum.TryParse(splittedInput[0], true, out Commands command))
            //{
            switch (splittedInput[0].ToCommand())
            {
                case Commands.Filter:
                    Filter();
                    break;
                case Commands.OrderBy:
                    OrderByPersonProperty(splittedInput.Length > 1 ? splittedInput[1] : null);
                    break;
                case Commands.Delete:
                    if(id.HasValue)
                        DeletePerson(id.Value);
                    break;
                case Commands.Add:
                    if(splittedInput.Length > 1)
                        switch (splittedInput[1])
                        {
                            case nameof(Student):
                                AddStudent();
                                break;
                            case nameof(Instructor):
                                AddInstructor();
                                break;
                        }
                    break;
                case Commands.Edit:
                    if (id.HasValue)
                        EditPerson(id.Value);
                    break;
                case Commands.Exit:
                    return false;
                default:
                    break;
            }   
            //}
            
            return true;
        }

        private static void Filter()
        {
            FilterFunc +=

                //TODO 2 Osoby z literą A w nazwisku
                //TODO 3 Studenci z wiekiem < 50
                //TODO 4 osoby z imieniem dłuższym niż 3 znaki lub z nazwiskiem na E
                //TODO 5 Instuktor z Ekonomii i osoba z id 5
        }

        private static void OrderByPersonProperty(string propertyName)
        {
            var property = typeof(Person).GetProperty(propertyName ?? string.Empty);
            if (property == null)
                OrderByFunc = null;
            else
                OrderByFunc += collection => collection.OrderBy(person => property.GetValue(person));
        }

        private static void DeletePerson(int id)
        {
            Context.Delete(id);
        }

        private static void AddInstructor()
        {
            var person = new Instructor();
            if (EditInstructor(person))
                Context.Create(person);

        }

        private static void AddStudent()
        {
            var person = new Student();
            if (EditStudent(person))
                Context.Create(person);
        }

        private static void EditPerson(int id)
        {
            var person = Context.Read(id);

            //if (person is Student student)
            //{
            //    if (!EditStudent(student))
            //        return;
            //}
            //else if (person is Instructor instructor)
            //{
            //    if (!EditInstructor(instructor))
            //        return;
            //}

            switch (person)
            {
                case Student student:
                    if (!EditStudent(student))
                        return;
                    break;
                case Instructor instructor:
                    if (!EditInstructor(instructor))
                        return;
                    break;
            }
            Context.Update(person);
        }


        private static bool EditStudent(Student student)
        {
            if(EditPerson(student))
            {
                var yearOfStudy = ReadPersonData(nameof(Student.YearOfStudy), student.YearOfStudy.ToString(), text => text.ToInt() == null);
                student.YearOfStudy = yearOfStudy.ToInt().Value;
                return true;
            }
            return false;
        }


        private static bool EditInstructor(Instructor instructor)
        {
            if (EditPerson(instructor))
            {
                var yearsOfWork = ReadPersonData(nameof(Instructor.YearsOfWork), instructor.YearsOfWork.ToString(), text => text.ToInt() == null);
                instructor.Specialization = ReadPersonData(nameof(Instructor.Specialization), instructor.Specialization, x => false);
                instructor.YearsOfWork = yearsOfWork.ToInt().Value;
                return true;
            }
            return false;
        }

        private static bool EditPerson(Person person)
        {
            if (person == null)
                return false;
            try
            {
                var firstName = ReadPersonData(nameof(Person.FirstName), person.FirstName, text => string.IsNullOrWhiteSpace(text));
                var lastName = ReadPersonData(nameof(Person.LastName), person.LastName, text => string.IsNullOrWhiteSpace(text));

                var birtDateString = ReadPersonData(nameof(Person.BithDate), person.BithDate.ToShortDateString(), text => 
                {
                    //DateTime dateTime;
                    //return !DateTime.TryParse(text, out dateTime);

                    return text.ToDateTime() == null;
                }
                );
                var genderString = ReadPersonData(nameof(Person.Gender), person.Gender.ToString(), text => !Enum.IsDefined(typeof(Genders), text));


                person.FirstName = firstName;
                person.LastName = lastName;
                //person.BithDate = DateTime.Parse(birtDateString);
                person.BithDate = birtDateString.ToDateTime().Value;
                person.Gender = (Genders)Enum.Parse(typeof(Genders), genderString);
            }
            catch(Exception e)
            {
                Debug.WriteLine(e);
                return false;
            }
            return true;
        }

        //* Func<string, bool> zastąpiło delegata PersonDataValidator
        //delegate bool PersonDataValidator(string input);

        static string ReadPersonData(string header, string currentValue, Func<string, bool> validator)
        {
            string input;
            do
            {
                TextOutput(header);
                SendKeys.SendWait(currentValue);
                input = Console.ReadLine();
            } while (validator?.Invoke(input) ?? true);
            return input;

        }

        private static void Example()
        {
            int? a = null;
            Nullable<int> b = 5;

            int c;
            if (a - b == 0)
                c = (a + b) ?? 0;
            else
            {
                var result = a - b;
                if (result.HasValue)
                    c = result.Value;
                else
                    c = 0;
            }
            c = (a - b == 0 ? a + b : a - b) ?? 0;
        }

        static void DisplayPeople()
        {

            //*1. wyświetlanie za pomocą budowania tekstu wyjściowego poprzez iterację po elementach
            //
            //List<string> personInfo = new List<string>();
            //foreach (var item in Context.Read())
            //{
            //    personInfo.Add(string.Format("{0, -3} {1, -15} {2, -15} {3, -10} {4, -15}",
            //        item.PersonId, item.FirstName, item.LastName, item.BithDate.ToShortDateString(), item.Gender));
            //}
            //var @string = string.Join("\n", personInfo);

            //*2. wykorzystanie LINQ QUERY do budowy wyświetlanego tekstu
            //
            //var @string = string.Join("\n", from person in Context.Read() select
            //                                string.Format("{0, -3} {1, -15} {2, -15} {3, -10} {4, -15}",
            //        person.PersonId, person.FirstName, person.LastName, person.BithDate.ToShortDateString(), person.Gender));


            var people = Context.Read();
            people = OrderByFunc?.Invoke(people) ?? people;
            //TODO 1 przefiltrowanie

            //if (OrderByFunc != null)
            //{
            //    people = OrderByFunc.Invoke(people);
            //}
            //else
            //    people = people;


            //3. wykorzystanie LINQ METHOD CHAIN do budowy wyświetlanego tekstu 
            var strings =  people.Select(person => person.ToString());
            var @string = string.Join("\n", strings);


            //*4. Zastąpienie string.Join metodą LINQ - Aggregate
            //
            //@string = Context.Read().Select(person => string.Format("{0, -3} {1, -15} {2, -15} {3, -10} {4, -15}",
            //        person.PersonId, person.FirstName, person.LastName, person.BithDate.ToShortDateString(), person.Gender))
            //        .Aggregate((a, b) => a + "\n" + b);

            //* Znak zapytania przed odwołaniem się do funkcji działa jak sprawdzenie czy obiekt nie jest null
            //if(TextOutput != null)
            TextOutput?.Invoke(@string);
        }

        static void WriteToConsole(string output)
        {
            Console.Clear();
            Console.WriteLine(output);
            Console.WriteLine();
        }

    }
}