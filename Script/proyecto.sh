#!/usr/bin/env bash

function ctrl_c() {
echo Cerrando Moogle!
cd Script
}

trap ctrl_c INT

if [ $# -eq 0 ]; then
echo "Selecione una opción"
echo "run - Abre la aplicación Moogle!"
echo "report - Genera informe de Moogle!"
echo "slides - Genera presentación de Moogle!"
echo "show_report - Muestra el informe de Moogle!"
echo "show_slides - Muestra la presentación de Moogle!"
echo "clean - Elimina archivos innecesarios que se crean en la ejecución de opciones anteriores"
echo "add - Añade archivos para buscar por Moogle!(se recomienda que estos sean .txt pues en caso contrario no impactarán en la búsqueda)"
echo "Salir"
read option
else
option="$1"
fi


while [ "$option" != "Salir" ]; do

if [ "$option" = "run" ]; then 
cd ..
make dev


elif [ "$option" = "report" ]; then 
cd ..
cd Informe
pdflatex moogle.tex
pdflatex moogle.tex

elif [ "$option" = "slides" ]; then 
cd ..
cd Presentación
pdflatex Presentación.tex

elif [ "$option" = "show_report" ]; then
cd ..
cd Informe
  if [ -f moogle.pdf ]; then
  echo "Introduzca comando del visualizador de su preferencia en caso de poseer ninguno introduzca 1"
  read lector
    if [ "$lector" != "1" ]; then
    $lector moogle.pdf
    else
    xdg-open moogle.pdf
    fi
  else 
  pdflatex moogle.tex
  pdflatex moogle.tex
  echo "Introduzca comando del visualizador de su preferencia en caso de poseer ninguno introduzca 1"
  read lector
    if [ "$lector" != "1" ]; then
    $lector moogle.pdf
    else
    xdg-open moogle.pdf
    fi
  fi

elif [ "$option" = "show_slides" ]; then
cd ..
cd Presentación
  if [ -f Presentación.pdf ]; then
  echo "Introduzca comando del visualizador de su preferencia en caso de poseer ninguno introduzca 2"
  read lector
    if [ "$lector" != "2" ]; then
    $lector Presentación.pdf
    else
    xdg-open Presentación.pdf
    fi
  else
  pdflatex Presentación.tex
  echo "Introduzca comando del visualizador de su preferencia en caso de poseer ninguno introduzca 2"
  read lector
    if [ "$lector" != "2" ]; then
    $lector Presentación.pdf
    else
    xdg-open Presentación.pdf
    fi
  fi
elif [ "$option" = "clean" ]; then 
cd ..
cd Informe
  if [ -f moogle.log ]; then
  rm -v moogle.log
  fi
  if [ -f moogle.aux ]; then
  rm -v moogle.aux
  fi
  if [ -f moogle.pdf ]; then
  rm -v moogle.pdf
  fi
  if [ -f moogle.fdb_latexmk ]; then
  rm -v moogle.fdb_latexmk
  fi
  if [ -f moogle.fls ]; then
  rm -v moogle.fls
  fi
  if [ -f moogle.synctex.gz ]; then
  rm -v moogle.synctex.gz
  fi

cd ..
cd Presentación
  if [ -f Presentación.aux ]; then
  rm -v Presentación.aux
  fi
  if [ -f Presentación.log ]; then
  rm -v Presentación.log
  fi
  if [ -f Presentación.nav ]; then
  rm -v Presentación.nav
  fi
  if [ -f Presentación.out ]; then
  rm -v Presentación.out
  fi
  if [ -f Presentación.pdf ]; then
  rm -v Presentación.pdf
  fi
  if [ -f Presentación.snm ]; then
  rm -v Presentación.snm
  fi
  if [ -f Presentación.toc ]; then
  rm -v Presentación.toc
  fi
cd ..
cd MoogleEngine
  if [ -d bin ]; then
  rm -r bin
  echo "bin borrado"
  fi
  if [ -d obj ]; then
  rm -r obj
  echo "obj borrado"
  fi
cd ..
cd MoogleServer
  if [ -d bin ]; then
  rm -r bin
  echo "bin borrado"
  fi
  if [ -d obj ]; then
  rm -r obj
  echo "obj borrado"
  fi

echo "Archivos eliminados"
elif [ "$option" = "add" ]; then
echo "Por favor introduzca ruta completa del archivo que desea agregar"
read ruta
  if [ -f "$ruta" ]; then

  cd ..
  cd Content
  cp "$ruta" .
  echo "Archivos copiados con éxito"
  else
  echo "Ruta inválida, revise la existencia del archivo"
  fi
else
echo "Solo son válidas las opciones listadas, por favor introduzca opción válida"
fi

echo "Selecione una opción"
echo "run - Abre la aplicación Moogle!"
echo "report - Genera informe de Moogle!"
echo "slides - Genera presentación de Moogle!"
echo "show_report - Muestra el informe de Moogle!"
echo "show_slides - Muestra la presentación de Moogle!"
echo "clean - Elimina archivos innecesarios que se crean en la ejecución de opciones anteriores"
echo "add - Añade archivos para buscar por Moogle!(se recomienda que estos sean .txt pues en caso contrario no impactarán en la búsqueda)"
echo "Salir"
read option

done
echo "Saliendo ..."

