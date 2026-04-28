using Microsoft.AspNetCore.Components;
using Invoqs.Models;
using Invoqs.Interfaces;

namespace Invoqs.Components.Pages
{
    public partial class BulkEmailHistory : ComponentBase
    {
        [Inject] private IBulkEmailLogService BulkEmailLogService { get; set; } = default!;

        private List<BulkEmailLogModel> logs = new();
        private BulkEmailLogModel? selectedLog;
        private bool isLoading = true;
        private bool isLoadingDetail = false;
        private string errorMessage = "";

        protected override async Task OnInitializedAsync()
        {
            try
            {
                logs = await BulkEmailLogService.GetAllLogsAsync();
            }
            catch (Exception ex)
            {
                errorMessage = $"Σφάλμα φόρτωσης ιστορικού: {ex.Message}";
            }
            finally
            {
                isLoading = false;
            }
        }

        private async Task ViewDetails(int logId)
        {
            isLoadingDetail = true;
            selectedLog = new BulkEmailLogModel(); // open modal immediately with spinner
            try
            {
                selectedLog = await BulkEmailLogService.GetLogByIdAsync(logId);
            }
            catch (Exception ex)
            {
                errorMessage = $"Σφάλμα φόρτωσης λεπτομερειών: {ex.Message}";
                selectedLog = null;
            }
            finally
            {
                isLoadingDetail = false;
            }
        }

        private void CloseModal()
        {
            selectedLog = null;
        }
    }
}
