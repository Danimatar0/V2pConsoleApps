// See https://aka.ms/new-console-template for more information
Console.WriteLine("Migrating data to redis");

try
{
    var coord = Helpers.Helpers.GetRandomCoordinate(1,5,1,5,1,5);
}catch (Exception e)
{
    Console.WriteLine(e.Message.ToString());
}
