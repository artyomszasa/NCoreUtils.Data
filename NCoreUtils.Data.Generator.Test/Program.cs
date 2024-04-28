// See https://aka.ms/new-console-template for more information
using NCoreUtils.Data;
using NCoreUtils.Data.Build;
using NCoreUtils.Data.Model;

var modelBuilder = new DataModelBuilder();
new MyContext().Apply(modelBuilder);
var model = new DataModel(modelBuilder);
Console.Write(model);
