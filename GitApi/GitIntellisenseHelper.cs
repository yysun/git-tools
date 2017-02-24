using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using GitScc;
using GitScc.DataServices;

namespace GitScc
{
    //Inspired by:
    //http://www.markembling.info/view/my-ideal-powershell-prompt-with-git-integration
    //https://github.com/dahlbyk/posh-git

    public class GitIntellisenseHelper
    {
        private static IEnumerable<string> GetGitData(GitRepository tracker, string option)
        {
            switch (option)
            {
                case "*branches*":
                    return tracker.RepositoryGraph.Refs
                        .Where(r => r.Type == RefTypes.Branch)
                        .Select(r => r.Name);

                case "*tags*":
                    return tracker.RepositoryGraph.Refs
                        .Where(r => r.Type == RefTypes.Tag)
                        .Select(r => r.Name);

                case "*remotes*":
                    return tracker.Remotes;

                case "*configs*":
                    return tracker.Configs.Keys;

                case "*commits*":
                    return tracker.RepositoryGraph.Commits
                        .OrderByDescending(c => c.AuthorDate)
                        .Select(r => r.ShortId);

                default:
                    return new string[] { };
            }

        }
        public static IEnumerable<string> GetOptions(GitRepository tracker, string command)
        {
            if (tracker == null) return new string[] { };
            var options = Commands.Where(i => Regex.IsMatch(command, i.Key)).Select(i => i.Value).FirstOrDefault();
            if (options == null) return new string[] { };

            if (options.Length==1 && options[0].Contains("|")) options = options[0].Split('|');

            var list = new List<string>();
            foreach(var option in options)
            {
                if (option.StartsWith("*"))
                {
                    list.AddRange(GetGitData(tracker, option));
                }
                else
                {
                    list.Add(option);
                }
            }
            return list;
        }

        static Dictionary<string, string[]> Commands = new Dictionary<string, string[]>{
            {"^git$", new string[] {"add", "bisect", "branch", "checkout", "commit", "config", "diff", "fetch", "format-patch", "grep", "init",  
                               "log", "merge", "mv", "pull", "push", "rebase", "remote", "reset", "rm", "show", "status", "stash", "tag"}},

            {"^git bisect$", new string[] {"start|bad|good|skip|reset|help"}},
            {"^git rebase$", new string[] {"*branches*"}},
            {"^git merge$", new string[] {"*branches*"}},
            {"^git rebase -i$", new string[] {"HEAD~"}},
            
            {"^git remote$", new string[] {"add|rename|rm|set-head|set-branches|set-url|show|prune|update"}},
            {"^git stash$", new string[] {"list|save|show|apply|drop|pop|branch|clear|create"}},
            //{"^git svn$", new string[] {"fetch|rebase|dcommit|info"}},

            {"^git checkout$", new string[] {"*branches*"}},
            {"^git branch -[dDmM]$", new string[] {"*branches*"}},
            {"^git tag -[asdfv]$", new string[] {"*tags*"}},
            {"^git tag .+$", new string[] {"*commits*"}},

            {"^git pull$", new string[] {"*remotes*"}},
            {"^git pull .+$", new string[] {"*branches*"}},
            {"^git push$", new string[] {"*remotes*"}},
            {"^git push .+$", new string[] {"*branches*"}},

            {"^git reset$", new string[] {"HEAD~|--soft|--mixed|--hard|--merge|--keep"}},
            {"^git reset HEAD$", new string[] {"*commits*"}},

            {"^git config$", new string[] {"--global|--system|--local|--get|--add|--unset|--list|-l|--file|*configs*"}},
            {"^git config\\s?(?:--global|--system|--local)?$", new string[] {"--get|--add|--unset|--list|*configs*"}},
            {"^git config\\s?(?:--global|--system|--local)?\\s?(?:--get|--add|--unset)$", new string[] {"*configs*"}},
        };
    }
}
