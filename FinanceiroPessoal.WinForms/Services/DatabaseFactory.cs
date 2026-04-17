using FinanceiroPessoal.WinForms.Repositories;
using System;
using System.Collections.Generic;
using System.Text;

namespace FinanceiroPessoal.WinForms.Services
{
    public static class DatabaseFactory
    {
        public static ILancamentoRepository CriarLancamentoRepository(TipoBanco tipo)
        {
            return tipo switch
            {
                TipoBanco.LocalSqlite => new SqliteLancamentoRepository(),
                TipoBanco.OnlineMySql => new MySqlLancamentoRepository(),
                _ => new SqliteLancamentoRepository()
            };
        }

        public static ICadastroAuxiliarRepository CriarCadastroAuxiliarRepository(TipoBanco tipo)
        {
            return tipo switch
            {
                TipoBanco.LocalSqlite => new SqliteCadastroAuxiliarRepository(),
                TipoBanco.OnlineMySql => new MySqlCadastroAuxiliarRepository(),
                _ => new SqliteCadastroAuxiliarRepository()
            };
        } 
    }
}
