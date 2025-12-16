#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
#pragma warning disable IDE1006 // Naming Styles


using GitHubActions.Gates.Framework.Models.API;

namespace GitHubActions.Gates.Framework.Models.WebHooks
{
    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage()]
    public class DeploymentProtectionRuleWebHook
    {
        public string action { get; set; }
        public string environment { get; set; }
        public string @event { get; set; }
        public string deployment_callback_url { get; set; }
        public Deployment deployment { get; set; }
        public object[] pull_requests { get; set; }
        public Repository repository { get; set; }
        public Organization organization { get; set; }
        public Sender sender { get; set; }
        public Installation installation { get; set; }
    }

    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage()]
    public class Deployment
    {
        public string url { get; set; }
        public long id { get; set; }
        public string node_id { get; set; }
        public string task { get; set; }
        public string original_environment { get; set; }
        public string environment { get; set; }
        public object description { get; set; }
        public DateTime created_at { get; set; }
        public DateTime updated_at { get; set; }
        public string statuses_url { get; set; }
        public string repository_url { get; set; }
        public Creator creator { get; set; }
        public string sha { get; set; }
        public string _ref { get; set; }
        public Payload payload { get; set; }
        public bool transient_environment { get; set; }
        public bool production_environment { get; set; }
        public Performed_Via_Github_App performed_via_github_app { get; set; }
    }

    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage()]
    public class Creator
    {
        public string login { get; set; }
        public long id { get; set; }
        public string node_id { get; set; }
        public string avatar_url { get; set; }
        public string gravatar_id { get; set; }
        public string url { get; set; }
        public string html_url { get; set; }
        public string followers_url { get; set; }
        public string following_url { get; set; }
        public string gists_url { get; set; }
        public string starred_url { get; set; }
        public string subscriptions_url { get; set; }
        public string organizations_url { get; set; }
        public string repos_url { get; set; }
        public string events_url { get; set; }
        public string received_events_url { get; set; }
        public string type { get; set; }
        public bool site_admin { get; set; }
    }

    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage()]
    public class Payload
    {
    }

    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage()]
    public class Performed_Via_Github_App
    {
        public long id { get; set; }
        public string slug { get; set; }
        public string node_id { get; set; }
        public Owner owner { get; set; }
        public string name { get; set; }
        public string description { get; set; }
        public string external_url { get; set; }
        public string html_url { get; set; }
        public DateTime created_at { get; set; }
        public DateTime updated_at { get; set; }
        public WebHookPermissions permissions { get; set; }
        public string[] events { get; set; }
    }

    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage()]
    public class WebHookPermissions
    {
        public string actions { get; set; }
        public string administration { get; set; }
        public string checks { get; set; }
        public string contents { get; set; }
        public string deployments { get; set; }
        public string discussions { get; set; }
        public string issues { get; set; }
        public string merge_queues { get; set; }
        public string metadata { get; set; }
        public string packages { get; set; }
        public string pages { get; set; }
        public string pull_requests { get; set; }
        public string repository_hooks { get; set; }
        public string repository_projects { get; set; }
        public string security_events { get; set; }
        public string statuses { get; set; }
        public string vulnerability_alerts { get; set; }
    }

    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage()]
    public class Repository
    {
        public long id { get; set; }
        public string node_id { get; set; }
        public string name { get; set; }
        public string full_name { get; set; }
        public bool _private { get; set; }
        public Owner1 owner { get; set; }
        public string html_url { get; set; }
        public object description { get; set; }
        public bool fork { get; set; }
        public string url { get; set; }
        public string forks_url { get; set; }
        public string keys_url { get; set; }
        public string collaborators_url { get; set; }
        public string teams_url { get; set; }
        public string hooks_url { get; set; }
        public string issue_events_url { get; set; }
        public string events_url { get; set; }
        public string assignees_url { get; set; }
        public string branches_url { get; set; }
        public string tags_url { get; set; }
        public string blobs_url { get; set; }
        public string git_tags_url { get; set; }
        public string git_refs_url { get; set; }
        public string trees_url { get; set; }
        public string statuses_url { get; set; }
        public string languages_url { get; set; }
        public string stargazers_url { get; set; }
        public string contributors_url { get; set; }
        public string subscribers_url { get; set; }
        public string subscription_url { get; set; }
        public string commits_url { get; set; }
        public string git_commits_url { get; set; }
        public string comments_url { get; set; }
        public string issue_comment_url { get; set; }
        public string contents_url { get; set; }
        public string compare_url { get; set; }
        public string merges_url { get; set; }
        public string archive_url { get; set; }
        public string downloads_url { get; set; }
        public string issues_url { get; set; }
        public string pulls_url { get; set; }
        public string milestones_url { get; set; }
        public string notifications_url { get; set; }
        public string labels_url { get; set; }
        public string releases_url { get; set; }
        public string deployments_url { get; set; }
        public DateTime created_at { get; set; }
        public DateTime updated_at { get; set; }
        public DateTime pushed_at { get; set; }
        public string git_url { get; set; }
        public string ssh_url { get; set; }
        public string clone_url { get; set; }
        public string svn_url { get; set; }
        public object homepage { get; set; }
        public int size { get; set; }
        public int stargazers_count { get; set; }
        public int watchers_count { get; set; }
        public object language { get; set; }
        public bool has_issues { get; set; }
        public bool has_projects { get; set; }
        public bool has_downloads { get; set; }
        public bool has_wiki { get; set; }
        public bool has_pages { get; set; }
        public bool has_discussions { get; set; }
        public int forks_count { get; set; }
        public object mirror_url { get; set; }
        public bool archived { get; set; }
        public bool disabled { get; set; }
        public int open_issues_count { get; set; }
        public object license { get; set; }
        public bool allow_forking { get; set; }
        public bool is_template { get; set; }
        public bool web_commit_signoff_required { get; set; }
        public object[] topics { get; set; }
        public string visibility { get; set; }
        public int forks { get; set; }
        public int open_issues { get; set; }
        public int watchers { get; set; }
        public string default_branch { get; set; }
    }

    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage()]
    public class Owner1
    {
        public string login { get; set; }
        public long id { get; set; }
        public string node_id { get; set; }
        public string avatar_url { get; set; }
        public string gravatar_id { get; set; }
        public string url { get; set; }
        public string html_url { get; set; }
        public string followers_url { get; set; }
        public string following_url { get; set; }
        public string gists_url { get; set; }
        public string starred_url { get; set; }
        public string subscriptions_url { get; set; }
        public string organizations_url { get; set; }
        public string repos_url { get; set; }
        public string events_url { get; set; }
        public string received_events_url { get; set; }
        public string type { get; set; }
        public bool site_admin { get; set; }
    }
    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage()]
    public class Organization
    {
        public string login { get; set; }
        public long id { get; set; }
        public string node_id { get; set; }
        public string url { get; set; }
        public string repos_url { get; set; }
        public string events_url { get; set; }
        public string hooks_url { get; set; }
        public string issues_url { get; set; }
        public string members_url { get; set; }
        public string public_members_url { get; set; }
        public string avatar_url { get; set; }
        public string description { get; set; }
    }

    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage()]
    public class Sender
    {
        public string login { get; set; }
        public long id { get; set; }
        public string node_id { get; set; }
        public string avatar_url { get; set; }
        public string gravatar_id { get; set; }
        public string url { get; set; }
        public string html_url { get; set; }
        public string followers_url { get; set; }
        public string following_url { get; set; }
        public string gists_url { get; set; }
        public string starred_url { get; set; }
        public string subscriptions_url { get; set; }
        public string organizations_url { get; set; }
        public string repos_url { get; set; }
        public string events_url { get; set; }
        public string received_events_url { get; set; }
        public string type { get; set; }
        public bool site_admin { get; set; }
    }

    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage()]
    public class Installation
    {
        public long id { get; set; }
        public string node_id { get; set; }
    }
}
