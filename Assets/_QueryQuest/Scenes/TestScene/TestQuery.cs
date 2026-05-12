using UnityEngine;
using QueryQuest.Database;

public class TestQuery : MonoBehaviour
{
    void Start()
    {
        var interpreter = new SQLInterpreter(DatabaseManager.Instance.DB);

        // Teste 1: Todos os feitiços
        var r1 = interpreter.Execute("SELECT * FROM Feiticos");
        Debug.Log(r1.FormattedLog);

        // Teste 2: Filtro por elemento
        var r2 = interpreter.Execute("SELECT * FROM Feiticos WHERE Elemento = 'Fogo'");
        Debug.Log(r2.FormattedLog);

        // Teste 3: AND composto
        var r3 = interpreter.Execute("SELECT * FROM Feiticos WHERE Elemento = 'Agua' AND Distancia = 'LONGO'");
        Debug.Log(r3.FormattedLog);

        // Teste 4: Erro de coluna
        var r4 = interpreter.Execute("SELECT * FROM Feiticos WHERE Distorcia = 'Fogo'");
        Debug.Log(r4.FormattedLog);
    }
}