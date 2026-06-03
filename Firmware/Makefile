.PHONY: setup shell

# Variables
IDF_PATH_LOCAL = $(HOME)/.espressif/v6.0/esp-idf
VENV = .venv
PYTHON = $(VENV)/bin/python

setup:
	@test -d $(VENV) || python3 -m venv $(VENV)
	@$(PYTHON) -m pip install --upgrade pip
	@$(PYTHON) -m pip install pytest-embedded pytest-embedded-serial-esp pytest-embedded-idf
	@if [ ! -d "$(HOME)/.espressif" ]; then \
		bash $(IDF_PATH_LOCAL)/install.sh; \
	fi
	@echo "✅ Configuración completada. Usa 'make shell' para entrar."

shell:
	@bash --rcfile <(echo "source ~/.bashrc; source $(VENV)/bin/activate; source $(IDF_PATH_LOCAL)/export.sh")
