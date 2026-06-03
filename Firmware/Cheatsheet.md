### Para instalar

Asegurarse que la ruta en 'Makefile' es la correcta para el esp-idf sdk. Ejecutar en el root

```
make setup
```

### Para correr la shell

Asegurarse que la ruta en 'Makefile' es la correcta para el esp-idf sdk. Ejecutar en el root

```
make shell
```

### Para correr tests

Desde la carpeta root del componente. Ejecutar desde la shell

```
idf.py set-target esp32s3
```

```
idf.py build
```

```
pytest --target esp32s3 -s -v
```