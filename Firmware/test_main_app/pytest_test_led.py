import pytest
from pytest_embedded_idf.dut import IdfDut 

# Indicamos a pytest que este test requiere un ESP32-S3 real
@pytest.mark.target('esp32s3')
@pytest.mark.env('generic')
def test_led_execution(dut: IdfDut) -> None:
# 1. El ESP32 arranca y Unity muestra su mensaje de bienvenida
    # Unity siempre imprime esta línea cuando entra a unity_run_menu()
    dut.expect_exact('Press ENTER to see the list of tests')
    
    # 2. Pytest le envía un comando al ESP32 por el puerto serie
    # Le decimos: "Ejecuta solo los casos de prueba que tengan el tag [led]"
    dut.write('[led]')
    
    # 3. Esperamos a ver que Unity confirme que empezó a correr esos tests
    # Ajusta esta línea a lo que imprima tu test internamente si es necesario
    # dut.expect_exact('Iniciando pruebas unitarias de LED') 
    
    # 4. Evaluamos el reporte final de Unity para la librería LED
    dut.expect(r'(\d+) Tests 0 Failures (\d+) Ignored', timeout=30)