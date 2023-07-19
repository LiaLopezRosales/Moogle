#!/usr/bin/env bash

echo "Si está ejecutando el script en Windows utilice alguna herramienta de compatibilidad para Linux"
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

while [ "$option" != "Salir" ]; do

if [ "$option" = "run" ]; then 
cd ..
make dev

elif [ "$option" = "report" ]; then 
cd ..
cd Informe
pdflatex moogle.tex

elif [ "$option" = "slides" ]; then 
cd ..
cd Presentación
pdflatex Presentación.tex

elif [ "$option" = "show_report" ]; then
cd ..
cd Informe
pdflatex moogle.tex
echo "Introduzca comando del visualizador de su preferencia en caso de poseer ninguno introduzca 1"
read lector
  if [ "$lector" != "1" ]; then
   $lector moogle.pdf
  else
  xdg-open moogle.pdf
  fi

elif [ "$option" = "show_slides" ]; then
cd ..
cd Presentación
pdflatex Presentación.tex
echo "Introduzca comando del visualizador de su preferencia en caso de poseer ninguno introduzca 2"
read lector
  if [ "$lector" != "2" ]; then
   $lector Presentación.pdf
  else
  xdg-open Presentación.pdf
  fi

elif [ "$option" = "clean" ]; then 
cd ..
cd Informe
  if [ -f moogle.log ]; then
  rm moogle.aux
  rm moogle.log
  rm moogle.pdf
  else
  echo "No existen archivos a eliminar del informe"
  fi
cd ..
cd Presentación
  if [ -f Presentación.log ]; then
  rm Presentación.aux
  rm Presentación.log
  rm Presentación.nav
  rm Presentación.out
  rm Presentación.pdf
  rm Presentación.snm
  rm Presentación.toc
  else
  echo "No existen archivos a eliminar de la presentación"
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
