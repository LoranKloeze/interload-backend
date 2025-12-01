// ReSharper disable InconsistentNaming
// ReSharper disable PropertyCanBeMadeInitOnly.Global
// ReSharper disable UnusedMember.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable UnassignedField.Global
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
namespace InterLoad.Models.ApiResponses;

public class MasterSyncUltraOfficeResponse
{

    public Articles[] articles { get; set; }
    public Projects[] projects { get; set; }
    public Stock_periods[] stock_periods { get; set; }
    public Sub_projects[] sub_projects { get; set; }
    public Sub_project_articles[] sub_project_articles { get; set; }
    
    public Packing_schemes[] packing_schemes { get; set; }
    public Packing_scheme_entries[] packing_scheme_entries { get; set; }
    
    public int max_sync_id { get; set; }


    public class Sub_projects
    {
        public int id { get; set; }
        public int project_id { get; set; }
        public string description { get; set; }
        public string created_at { get; set; }
        public string updated_at { get; set; }
        public int position { get; set; }
        public int packing_group { get; set; }
        public bool self_service { get; set; }
        public object remarks { get; set; }
        public bool is_optional { get; set; }
        public string last_import_notes { get; set; }
        public int? prep_list_for_sub_project_id { get; set; }
        public bool is_spare_list { get; set; }
        public bool is_default_spare_list { get; set; }
        public int sync_id { get; set; }
    }

    public class Articles
    {
        public int id { get; set; }
        public string description { get; set; }
        public string location { get; set; }
        public decimal? weight { get; set; }
    }

    public class Projects
    {
        public string? customer_name;
        public int id { get; set; }
        public string name { get; set; }
        public string begin_at { get; set; }
        public string end_at { get; set; }
        public string? rentman_ref { get; set; }
        
        public int? active_packing_scheme_id { get; set; }
    }

    public class Stock_periods
    {
        public int id { get; set; }
        public string reference { get; set; }
        public int project_id { get; set; }
        public string begin_at { get; set; }
        public string end_at { get; set; }
        public string created_at { get; set; }
        public string updated_at { get; set; }
        public int position { get; set; }
        public int sync_id { get; set; }
    }

    public class Sub_project_articles
    {
        public int id { get; set; }
        public int? article_id { get; set; }
        public int sub_project_id { get; set; }
        public int stock_period_id { get; set; }
        public int primary_amount { get; set; }
        public int secondary_amount { get; set; }
        public string remark { get; set; }
        public int sync_id { get; set; }
    }
    
    public class Packing_schemes
    {
        public int id { get; set; }
        public bool merge_stock_periods { get; set; }
        public string title { get; set; }
        public int project_id { get; set; }
        public string created_at { get; set; }
        public string updated_at { get; set; }
        public int sync_id { get; set; }
    }
    
    public class Packing_scheme_entries
    {
        public int id { get; set; }
        public int group_nr { get; set; }
        public int sub_project_id { get; set; }
        public int packing_scheme_id { get; set; }
        public string created_at { get; set; }
        public string updated_at { get; set; }
        public int sync_id { get; set; }
    }
}