using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace FinanceiroPessoal.WinForms.Models
{
    public class GastoCategoriaDto
    {
        public string Categoria { get; set; } = string.Empty;
        public decimal Valor { get; set; }
    }
}
