# Thin wrapper around scripts/*.py.  Run `make help` for usage.
PYTHON ?= python3
MOD ?=
FLAVOR ?=
VERSION ?=
TARGET ?=

.DEFAULT_GOAL := help
.PHONY: help build test bundle deploy log pull clean clean-logs

help:
	@echo "make build   [MOD=<mod>] [FLAVOR=mono|il2cpp]   build (default: every mod and flavor)"
	@echo "make test    [MOD=<mod>]                        run the unit tests (default: every mod)"
	@echo "make bundle  MOD=<mod> [FLAVOR=<f>] [VERSION=<v>] build and zip to out/bundles/"
	@echo "make deploy  MOD=<mod> [FLAVOR=<f>] [TARGET=<t>]  build and scp the DLL to a target (etc/local.json)"
	@echo "make log     [TARGET=<t>]                       fetch and print MelonLoader's Latest.log"
	@echo "make pull    [FLAVOR=<f>] [TARGET=<t>]          copy game assemblies into lib/ from the source machine"
	@echo "make clean                                       remove out/"
	@echo "make clean-logs                                  remove logs/"

build:
	$(PYTHON) scripts/build.py $(if $(MOD),-m $(MOD)) $(if $(FLAVOR),-f $(FLAVOR))

test:
	$(PYTHON) scripts/test.py $(if $(MOD),-m $(MOD))

bundle: require-mod
	$(PYTHON) scripts/bundle.py -m $(MOD) $(if $(FLAVOR),-f $(FLAVOR)) $(if $(VERSION),-V $(VERSION))

deploy: require-mod
	$(PYTHON) scripts/deploy.py -m $(MOD) $(if $(FLAVOR),-f $(FLAVOR)) $(if $(TARGET),-t $(TARGET))

log:
	$(PYTHON) scripts/log.py $(if $(TARGET),-t $(TARGET))

pull:
	$(PYTHON) scripts/pull.py $(if $(FLAVOR),-f $(FLAVOR)) $(if $(TARGET),-t $(TARGET))

clean:
	rm -rf out

clean-logs:
	rm -rf logs

.PHONY: require-mod
require-mod:
	@test -n "$(MOD)" || { echo "usage: make $(MAKECMDGOALS) MOD=<mod> [FLAVOR=<mono|il2cpp>]" >&2; exit 2; }
