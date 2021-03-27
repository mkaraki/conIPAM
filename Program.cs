using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace conIpam
{
    class Program
    {
        static void Main(string[] args)
        {
            string defaultpath = System.IO.Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, "default.json");
            IpFile f;
            if (System.IO.File.Exists(defaultpath))
            {
                System.Console.Write("Loading File");
                string json = System.IO.File.ReadAllText(defaultpath, System.Text.Encoding.UTF8);
                f = JsonSerializer.Deserialize<IpFile>(json);
                System.Console.WriteLine("\rLoaded File!");
            }
            else
            {
                System.Console.WriteLine("No IPAM file. Create it.");
                f = new IpFile();
            }


            Stack<IpRange> editTarget = new Stack<IpRange>();

            while (true)
            {
                if (editTarget.Count == 0)
                {
                    System.Console.Write("4 6 ? ");
                    switch (Console.ReadKey().KeyChar)
                    {
                        case '4':
                            editTarget.Push(f.IPv4);
                            break;

                        case '6':
                            editTarget.Push(f.IPv6);
                            break;
                    }

                    Console.WriteLine();
                    continue;
                }

                System.Console.Write("[{0}] Show Add New Edit Del Go Back eXit saVe Help ? ", editTarget.Peek().RangeName);
                var akey = Console.ReadKey().KeyChar;
                Console.WriteLine();
                switch (akey)
                {
                    case 's':
                        ShowAllEntries(editTarget.Peek());
                        break;

                    // 
                    case 'a':
                        AddHost(editTarget.Peek());
                        break;

                    // New
                    case 'n':
                        AddRange(editTarget.Peek());
                        break;

                    case 'e':
                        Edit(editTarget.Peek());
                        break;

                    case 'd':
                        Delete(editTarget.Peek());
                        break;

                    case 'g':
                        GotoRange(ref editTarget);
                        break;

                    case 'b':
                        editTarget.Pop();
                        break;

                    case 'x':
                        Save(f, defaultpath);
                        Environment.Exit(0);
                        break;

                    case 'v':
                        Save(f, defaultpath);
                        break;

                    case 'h':
                        WriteHelp();
                        break;
                }

            }
        }

        public static void GotoRange(ref Stack<IpRange> stack)
        {
            var p = stack.Peek();

            for (int i = 0; i < p.InnerRanges.Count; i++)
            {
                System.Console.WriteLine("{0}: {1}\t{2}", (i + 1).ToString().PadLeft(4), p.InnerRanges[i].RangeName, p.InnerRanges[i].Description ?? string.Empty);
            }
            Console.Write("> ");
            var ri = Console.ReadLine();
            if (!int.TryParse(ri, out int index) || index > p.InnerRanges.Count || index <= 0)
            {
                System.Console.WriteLine("Not Valid");
                return;
            }

            stack.Push(p.InnerRanges[index - 1]);
        }

        public static void Save(IpFile f, string path)
        {
            System.Console.Write("Saving");
            string json = JsonSerializer.Serialize(f);
            System.IO.File.WriteAllText(path, json, System.Text.Encoding.UTF8);
            System.Console.WriteLine("\rSaved!");
        }

        public static void ShowAllEntries(IpRange target)
        {
            int gi = 1;

            for (int i = 0; i < target.InnerRanges.Count; i++)
            {
                System.Console.WriteLine("{0} R {1}\t{2}\t{3}", gi.ToString().PadLeft(4), target.InnerRanges[i].IpWithCidr, target.InnerRanges[i].RangeName, target.InnerRanges[i].Description ?? string.Empty);
                gi++;
            }

            for (int i = 0; i < target.Hosts.Count; i++)
            {
                System.Console.WriteLine("{0} H {1}\t{2}\t{3}", gi.ToString().PadLeft(4), target.Hosts[i].Ip, target.Hosts[i].Hostname, target.Hosts[i].Description ?? string.Empty);
                gi++;
            }

        }

        public static bool Confirm(string message, bool def)
        {
            Console.Write($"{message} [{(def ? 'Y' : 'y')}es|{(def ? 'n' : 'N')}o] ?");
            var c = Console.ReadKey();
            Console.WriteLine();
            if (c.Key == ConsoleKey.Enter) return def;

            switch (c.KeyChar)
            {
                case 'y':
                    return true;

                case 'n':
                    return false;

                default:
                    return Confirm(message, def);
            }
        }

        public static void Delete(IpRange target)
        {
            ShowAllEntries(target);

            Console.Write("> ");
            var ri = Console.ReadLine();
            if (!int.TryParse(ri, out int index) || index > target.Hosts.Count + target.InnerRanges.Count || index <= 0)
            {
                System.Console.WriteLine("Not Valid");
                return;
            }

            index--;
            int aindex = index < target.InnerRanges.Count ? index : index - target.InnerRanges.Count;
            if (index < target.InnerRanges.Count)
            {
                var c = target.InnerRanges[index];
                if (!Confirm($"Are you sure delete {c.RangeName}? All inner hosts/ranges will deleted.", false)) return;
                target.InnerRanges.Remove(c);
            }
            else
            {
                index -= target.InnerRanges.Count;
                var c = target.Hosts[index];
                if (!Confirm($"Are you sure delete {c.Hostname}", false)) return;
                target.Hosts.Remove(c);
            }
        }

        public static void Edit(IpRange target)
        {
            ShowAllEntries(target);

            Console.Write("> ");
            var ri = Console.ReadLine();
            if (!int.TryParse(ri, out int index) || index > target.Hosts.Count + target.InnerRanges.Count || index <= 0)
            {
                System.Console.WriteLine("Not Valid");
                return;
            }

            index--;
            if (index < target.InnerRanges.Count)
                EditRange(target, index);
            else
                EditHost(target, index - target.InnerRanges.Count);
        }

        public static void EditHost(IpRange targetRange, int index)
        {
            var c = targetRange.Hosts[index];

            Console.Write($"Name [{c.Hostname}]> ");
            var name = Console.ReadLine();
            name = name != string.Empty ? name : c.Hostname;
            Console.Write($"Desc [{c.Description ?? string.Empty}]> ");
            var desc = Console.ReadLine();
            desc = desc != string.Empty ? desc : c.Description;
            desc = desc == " " ? null : desc;

            c.Hostname = name;
            c.Description = desc;
        }

        public static void EditRange(IpRange targetRange, int index)
        {
            var c = targetRange.InnerRanges[index];

            Console.Write($"Name [{c.RangeName}]> ");
            var name = Console.ReadLine();
            name = name != string.Empty ? name : c.RangeName;
            Console.Write($"Desc [{c.Description ?? string.Empty}]> ");
            var desc = Console.ReadLine();
            desc = desc != string.Empty ? desc : c.Description;
            desc = desc == " " ? null : desc;

            c.RangeName = name;
            c.Description = desc;
        }

        public static void AddHost(IpRange targetRange)
        {
            Console.Write("IP> ");
            var ip = Console.ReadLine();
            Console.Write("Name> ");
            var name = Console.ReadLine();
            Console.Write("Desc []> ");
            var desc = Console.ReadLine();
            if (desc == string.Empty) desc = null;

            try
            {
                var iph = new IpHost(ip, name, desc);
                if (!targetRange.IsAddressInRange(iph))
                {
                    System.Console.WriteLine("Not in range of {0}", targetRange.RangeName);
                    return;
                }
                if (targetRange.IsAddressInChildRange(iph))
                {
                    System.Console.WriteLine("Duplicated with Child Range");
                    return;
                }
                if (targetRange.Hosts.Any(v => v.Ip == ip))
                {
                    System.Console.WriteLine("Already found.");
                    return;
                }

                targetRange.Hosts.Add(iph);
            }
            catch
            {
                System.Console.WriteLine("not valid Ip");
            }
        }

        public static void AddRange(IpRange targetRange)
        {
            Console.Write("IP(CIDR)> ");
            var ip = Console.ReadLine();
            Console.Write("Name> ");
            var name = Console.ReadLine();
            Console.Write("Desc []> ");
            var desc = Console.ReadLine();
            if (desc == string.Empty) desc = null;

            try
            {
                var ipr = new IpRange(ip, name, desc);
                if (!targetRange.IsRangeInRange(ipr))
                {
                    System.Console.WriteLine("Not in range of {0}", targetRange.RangeName);
                    return;
                }
                if (targetRange.IsRangeInChildRange(ipr))
                {
                    System.Console.WriteLine("Duplicated Range");
                    return;
                }
                if (targetRange.InnerRanges.Any(v => v.IpWithCidr == ipr.IpWithCidr))
                {
                    System.Console.WriteLine("Already found");
                    return;
                }

                targetRange.InnerRanges.Add(ipr);
            }
            catch
            {
                System.Console.WriteLine("not valid Ip");
            }
        }

        public static void WriteHelp()
        {
            System.Console.WriteLine(@"Help:
Add: Add new host entry.
New: Add new IP Range entry.
Edit: Edit host/range name/description.
delete: Delete host/range.
Open: Open range.
Close: Back parent range.
Exit: Save and Exit Application.
Help: This help.");
        }
    }
}
