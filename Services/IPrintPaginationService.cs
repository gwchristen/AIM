using System;
using System.Collections.Generic;
using System.Text;
using AIM.Models;

namespace AIM.Services;

public interface IPrintPaginationService
{
    List<PrintablePage> PaginateContent(
        string pageHeader,
        //string level2Header,
        List<PrintableFormItem> allRows);
}
