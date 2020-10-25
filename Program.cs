using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;

namespace GeneticNeuronalNetwork
{
    class Program
    {
        static async Task Main(string[] args)
        {
            /// Declara los articulos
            List<Article> products = new List<Article>();
            var Milk = new Article("Milk", 0.5, 500);
            products.Add(Milk);
            var Cookie = new Article("Cookie", 0.1, 300);
            products.Add(Cookie);
            var Water = new Article("Water", 0.5, 100);
            products.Add(Water);
            var BreadChiken = new Article("BreadChiken", 0.25, 700);
            products.Add(BreadChiken);
            var Egg = new Article("Egg", 0.15, 300);
            products.Add(Egg);
            var Walnuts = new Article("Walnuts", 0.15, 400);
            products.Add(Walnuts);
            var Yogurt = new Article("Yogurt", 0.5, 500);
            products.Add(Yogurt);
            var Apple = new Article("Apple", 0.3, 400);
            products.Add(Apple);

            try
            {
                /// Muestra la lista de productos
                Console.WriteLine("Products for Backpack:");
                Console.WriteLine("#######################");
                foreach (var p in products)
                {
                    Console.WriteLine("ProductName: {0}, Weight: {1}, Calories: {2}", p.Name, p.Weight, p.Calories);
                }
                Console.WriteLine("#######################");

                #region Lectura de los parametros de configuración de la RED
                /// Solicita los cromosomas a generar para la primera iteración
                int noChromosomes = 0;
                while (noChromosomes <= 0)
                {
                    try
                    {
                        Console.WriteLine("Ingrese el número de cromosomas de la red neuronal genetica: ");
                        Console.WriteLine("Valor mayor a 0:");
                        noChromosomes = Convert.ToInt32(Console.ReadLine());
                        if (noChromosomes <= 0)
                            noChromosomes = 0;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("Valor ingresado es incorrecto, debe ser un numero sin decimales.");
                    }
                }


                /// Ingresa la puntuación meta para definir el score/fitness final para terminar la iteración
                int score = 0;
                while (score <= 0)
                {
                    try
                    {
                        Console.WriteLine("Ingrese el valor del Fitness o Score aceptable para terminar las iteraciones de la red.");
                        Console.WriteLine("Valores entre el 30% y el 100%:");
                        score = Convert.ToInt32(Console.ReadLine());
                        if (score < 30)
                            score = 0;
                        if (score > 100)
                            score = 0;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("Valor ingresado es incorrecto, debe ser un numero sin decimales.");
                    }
                }

                /// % se mejores candidatos
                int percentSelection = 0;
                while (percentSelection <= 0)
                {
                    try
                    {
                        Console.WriteLine("Ingrese el % de sujetos a seleccionar en la seleccion de los mejores candidatos. ");
                        Console.WriteLine("Valores mayores al 10% recomendados:");
                        percentSelection = Convert.ToInt32(Console.ReadLine());
                        if (percentSelection < 10)
                            percentSelection = 0;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("Valor ingresado es incorrecto, debe ser un numero sin decimales.");
                    }
                }
                #endregion



                // Construcción de la RED neuronal Genetica
                Console.WriteLine("Construcción de la red genetica.");
                GeneticNeuronalNetwork red = new GeneticNeuronalNetwork(noChromosomes, products, score, percentSelection);



                #region Primera iteración
                int iterator = 1;
                Console.WriteLine("################ Generación {0}", iterator);
                // Genera los cromosomas segun los criterios de peso y calorias
                await red.GenerateChromosomes();

                // Obtiene el score o Fitness
                await red.Fitnes();

                // Realiza la seleccion a un porciento dado de cromosomas
                // leer el % de la selección
                //Console.WriteLine("Selecting the chromosomes...");
                await red.Selection(); // 80% de los mejores fitness preferentemente

                // Cruze y reproducción
                //Console.WriteLine("Cruze and reproduction...");
                await red.CrossOver();

                // Mutación
                //Console.WriteLine("Mutation...");
                await red.Mutate();

                // Se revisan los cromosomas despues de la mutación para ver si cumplen con los criterios de aceptación de peso
                // y calorias, en caso de no cumplir, esos cromosomas se desechan. Para cada cromosoma desechado se genera 
                // otro cromosoma aleatorio que cumpla con los criterios de aceptación de peso y calorias.
                await red.CheckAceptanceCriteria();

                #endregion


                #region Iteraciones continuas
                iterator++;
                while (true)
                {
                    Console.WriteLine("################ Generación {0}", iterator);
                    /// Valida el score fitness de la selección para saber si algun elemento 
                    /// cumplio con el score de aceptación, sin no cumple se realiza el cruze, reproducción
                    /// y mutación para nuevos cromosomas.
                    Chromosome aceptable = red._chromosomes.FirstOrDefault(I => I.FitnessTotal >= red._score);
                    if (aceptable != null)
                    {
                        Console.WriteLine("El Cromosoma aceptable para la condición contiene la combinación de: ");
                        foreach (var p in aceptable.Products)
                        {
                            string intoBackPack = p.Key == 1 ? "SI" : "NO";
                            Console.WriteLine("{0}: {1}", p.Value.Name, intoBackPack);
                        }
                        red.PrintChromosomes();
                        break; // TERMINA LA ITERACIÓN
                    }
                    else
                    {
                        // Si no cumple la condición realiza el cruze, reproducción y mutación, y repite el ciclo
                        // Cruze y reproducción
                        //Console.WriteLine("Cruze and reproduction...");
                        await red.CrossOver();

                        // Mutación
                        //Console.WriteLine("Mutation...");
                        await red.Mutate();

                        // Check
                        await red.CheckAceptanceCriteria();

                        // Obtiene el score o Fitness
                        await red.Fitnes();

                        // Realiza la seleccion a un porciento dado de cromosomas
                        //Console.WriteLine("Selecting the chromosomes...");
                        await red.Selection(); // 80% de los mejores fitness
                    }
                    iterator++;
                    //Thread.Sleep(100);
                }
                #endregion
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }

            /// Precione una tecla para salir para Salir
            Console.WriteLine("Presione cualquier tecla para terminar.");
            var exit = Console.ReadLine();
        }
    }

    public class GeneticNeuronalNetwork
    {
        public int _populationSize;
        public List<Article> _products;

        private ChromosomeFactory _factory;
        public List<Chromosome> _chromosomes;

        private double WeightTotal = 0.0;
        private double CaloriesTotal = 0;

        public int _score;
        public int _percentSelection;

        public GeneticNeuronalNetwork(int populationSize, List<Article> products, int score, int percentSelection)
        {
            _populationSize = populationSize;
            _products = products;
            _score = score;
            _percentSelection = percentSelection;

            _chromosomes = new List<Chromosome>();
            _factory = new ChromosomeFactory();
        }

        /// <summary>
        /// Genera el número de cromosomas de la red neuronal
        /// </summary>
        public async Task GenerateChromosomes()
        {
            Console.WriteLine("Generation...");
            // Generar los cromosomas
            for (int i = 0; i < _populationSize; i++)
            {
                _chromosomes.Add(_factory.GetChromosome(_products));
            }
            Console.Write("Chromosomes list: ");
            PrintChromosomes();
        }

        /// <summary>
        /// Calcula el Fitnes de los cromosomas
        /// </summary>
        public async Task Fitnes()
        {
            Console.WriteLine("Fitness...");
            var sumweight = _chromosomes.Sum(C => C.Weight);
            WeightTotal = Math.Round(sumweight, 2);
            CaloriesTotal = _chromosomes.Sum(C => C.Calories);

            foreach (var c in _chromosomes)
            {
                var fw = (c.Weight * 100) / WeightTotal;
                var fc = (c.Calories * 100) / CaloriesTotal;

                c.FitnessWeight = Math.Round(fw, 2);
                c.FitnessCalories = Math.Round(fc, 2);
                c.FitnessTotal = c.FitnessWeight + c.FitnessCalories;
            }

            //Parallel.ForEach(_chromosomes, c =>
            //{

            //});

            Console.Write("Chromosomes list: ");
            PrintChromosomes();
        }

        /// <summary>
        /// Selecciona los mejores cromosomas. selecciona los mejores cromosomas segun si scrore fitness
        /// </summary>
        /// <returns></returns>
        public async Task Selection()
        {
            Console.WriteLine("Selecction...");
            _chromosomes = _chromosomes.OrderByDescending(C => C.FitnessTotal).ToList();
            int indextoSelection = (_percentSelection * _chromosomes.Count) / 100;
            _chromosomes = _chromosomes.GetRange(0, indextoSelection);
            
            Console.Write("Chromosomes list: ");
            PrintChromosomes();
        }

        public async Task CrossOver()
        {
            Console.WriteLine("Cruze and Reproduction...");
            List<Chromosome> chromosomesReproduction = new List<Chromosome>();

            /// Cruza cada cromosoma con su siguiente
            for (int i = 0; i < _chromosomes.Count; i++)
            {
                var firts = _chromosomes[i];
                int next = i + 1;
                // el indice del segundo elemento es el primero cuando la iteración va en el ultimo elemento 
                //  de la lista
                if (next >= _chromosomes.Count)
                    next = 0;
                var second = _chromosomes[next];

                var newChromosome = _factory.GenerateFromCrossOver(firts, second);
                chromosomesReproduction.Add(newChromosome);
            }

            // invierte el orden de la lista para generar los cromosomas faltantes
            _chromosomes.Reverse();
            /// Cruza cada cromosoma con su siguiente
            for (int i = 0; i < _chromosomes.Count; i++)
            {
                /// si ya tenemos de nuevo la población terminamos el proceso de cruze y reproduccion
                if (chromosomesReproduction.Count == _populationSize)
                    break;

                var firts = _chromosomes[i];
                int next = i + 1;
                // el indice del segundo elemento es el primero cuando la iteración va en el ultimo elemento 
                // de la lista
                if (next >= _chromosomes.Count)
                    next = 0;
                var second = _chromosomes[next];

                var newChromosome = _factory.GenerateFromCrossOver(firts, second);
                chromosomesReproduction.Add(newChromosome);
            }

            // Update main chromosomes list
            _chromosomes = chromosomesReproduction;

            Console.Write("Chromosomes list: ");
            PrintChromosomes();
        }

        public async Task Mutate()
        {
            Console.WriteLine("Mutation...");
            foreach (var c in _chromosomes)
            {
                // genera un numero aleatorio para la mutación
                Random rand = new Random();
                int randomPosition = rand.Next(0, c.Products.Count - 1);
                // Modifica si trae o no el articulo/producto en la posicion random
                var p = c.Products[randomPosition];
                if (p.Key == 0)
                    c.Products[randomPosition] = new KeyValuePair<int, Article>(1, p.Value);
                else if (p.Key == 1)
                    c.Products[randomPosition] = new KeyValuePair<int, Article>(0, p.Value);
            }

            Console.Write("Chromosomes list: ");
            PrintChromosomes();
        }

        public async Task CheckAceptanceCriteria()
        {
            Console.WriteLine("Verifications...");

            /// Verifica cada cromosoma que cumpla con los criterios 
            /// si no cumple un cromosoma se elimina y se genera un nuevo cromosoma
            List<Chromosome> renewChromosomes = new List<Chromosome>();
            foreach (var c in _chromosomes)
            {
                // checa que cumpla lo criterios de peso y calorias

                double weight = 0.0;
                int calories = 0;
                //bool isAcceptance = false;
                foreach (var p in c.Products)
                {
                    weight += p.Value.Weight;
                    calories += p.Value.Calories;
                }
                if (weight <= 2 && calories >= 2000)
                {
                    /// el cromosoma es aceptable y cumple los criterios
                    /// de peso y calorias
                    //isAcceptance = true;
                    renewChromosomes.Add(c);
                }
                else
                {
                    //isAcceptance = false;
                    // Si el cromosoma no es aceptable, se elimina de la lista principal
                    // y se genera un nuevo cromosoma aleatorio para sustituir el eliminado.
                    Chromosome newchromosome = _factory.GetChromosome(_products);
                    renewChromosomes.Add(newchromosome);
                }
            }

            /// Actualiza la colección principal de cromosomas por la nueva colección generada
            _chromosomes = renewChromosomes;

            Console.Write("Chromosomes list: ");
            PrintChromosomes();
        }

        public void PrintChromosomes()
        {
            foreach (var c in _chromosomes)
            {
                string log = string.Empty;
                log += "[";
                foreach (var p in c.Products)
                {
                    log += string.Format("{0},", p.Key);
                }
                log += "]";
                log += string.Format(", Weight = [{0}]", c.Weight);
                log += string.Format(", Calories = [{0}]", c.Calories);
                log += string.Format(", FitnessWeight = [{0}]", c.FitnessWeight);
                log += string.Format(", FitnessCalories = [{0}]", c.FitnessCalories);
                log += string.Format(", FitnessTotal = [{0}] ;", c.FitnessTotal);
                Console.WriteLine(log);
            }
        }
    }
}


/// <summary>
/// Genera cromosomas en base a los productos.
/// Las condiciones para generar un cromosoma son que no  pase de 2 KG y sea mayor a 2000 calorías
/// </summary>
public class ChromosomeFactory
{
    public List<Article> _products;

    /// <summary>
    /// Genera el cromosoma
    /// </summary>
    /// <param name="products"></param>
    /// <returns></returns>
    public Chromosome GetChromosome(List<Article> products)
    {
        _products = products;
        Chromosome c = null;
        while (c is null)
        {
            c = Generate();
        }
        return c;
    }

    /// <summary>
    /// Cruza y reproduce 2 cromosomas dados
    /// </summary>
    /// <param name="c1"></param>
    /// <param name="c2"></param>
    /// <returns></returns>
    public Chromosome GenerateFromCrossOver(Chromosome c1, Chromosome c2)
    {
        // obtengo el numero aleatorio para la partición/cruze
        Random rand = new Random();
        int randomPosition = rand.Next(1, 5); // entre las posiciones 1 y 5

        // obtengo el rango de productos del primer cromosoma. 
        // Partición del primer cromosoma
        var c1_rangeProducts = c1.Products.GetRange(0, randomPosition);

        int index = c1_rangeProducts.Count;
        int items = c2.Products.Count - index;

        // Particion del segundo cromosoma
        var c2_randomProduts = c2.Products.GetRange(index, items);

        // Reproducción del nuevo cromosoma
        Chromosome newChromosome = new Chromosome();
        newChromosome.Products = new List<KeyValuePair<int, Article>>();
        newChromosome.Products.AddRange(c1_rangeProducts);
        newChromosome.Products.AddRange(c2_randomProduts);

        return newChromosome;
    }

    /// <summary>
    /// Genera un chromosoma aleatoriamente
    /// </summary>
    /// <returns></returns>
    private Chromosome Generate()
    {
        // diccionario de articulos define cuales llevar y cuales no
        List<KeyValuePair<int, Article>> list = new List<KeyValuePair<int, Article>>();
        double weight = 0.0;
        int calories = 0;
        Random rand = new Random();
        // Genera combinaciones de productos en la mochila hasta complir la condición de peso y calorias
        foreach (var prod in _products)
        {
            int isLoad = rand.Next(0, 2);
            if (isLoad == 1)
            {
                list.Add(new KeyValuePair<int, Article>(1, prod));
            }
            else
            {
                list.Add(new KeyValuePair<int, Article>(0, prod));
            }
        }

        /// Calcula el peso y calorias totales del cromosoma
        foreach (var entry in list)
        {
            if (entry.Key == 1)
            {
                weight += entry.Value.Weight;
                calories += entry.Value.Calories;
            }
        }

        /// Crea el nuevo cromosoma si el peso y calorias son aceptables
        if (weight <= 2 && calories >= 2000)
        {
            Chromosome chromo = new Chromosome();
            chromo.Products = list;
            chromo.Weight = weight;
            chromo.Calories = calories;

            return chromo;
        }
        else
        {
            return null;
        }
    }
}


/// <summary>
/// Estructura de un producto/articulo
/// </summary>
public class Article
{
    public Article(string name, double weight, int calories)
    {
        this.Name = name;
        this.Weight = weight;
        this.Calories = calories;
    }

    public string Name { get; set; }
    public double Weight { get; set; }
    public int Calories { get; set; }
}

/// <summary>
/// Estructura de los cromosomas
/// </summary>
public class Chromosome
{
    public List<KeyValuePair<int, Article>> Products { get; set; }
    public double Weight { get; set; }
    public int Calories { get; set; }
    public double FitnessWeight { get; set; }
    public double FitnessCalories { get; set; }
    public double FitnessTotal { get; set; }
}

