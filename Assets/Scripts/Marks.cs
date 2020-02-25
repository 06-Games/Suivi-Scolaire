using UnityEngine;

public class Marks : MonoBehaviour
{
    public static Note[] Notes;
}

public class Note
{
    public string nom;
    public string codePeriode;
    public string codeMatiere;
    public string libelleMatiere;
    public string codeSousMatiere;
    public string typeDevoir;
    public bool enLettre;
    public float coef;
    public float noteSur;
    public float? valeur;
    public bool nonSignificatif;
    public System.DateTime date;
    public System.DateTime dateSaisie;
    public bool valeurisee;
    public float? moyenneClasse;
    public Competence[] competences;
}

public class Competence
{
    public string nom;
    public uint? id;
    public string valeur;
    public bool cdt;
    public uint idCat;
    public string libelleCat;
}