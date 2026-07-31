# Lógica Polytrend (Extraída de NotebookLM)

Esta es la lógica detallada recuperada de la sesión anterior sobre el sistema **Trader Sumo / Polytrend**.

## 1. El Pilar: El Patrón A-B
- **Punto A (Soporte/Resistencia de Origen):** Es el *swing* (mínimo en tendencia alcista, máximo en bajista) que dio origen al movimiento que rompió un extremo previo.
- **Punto B (Objetivo de Tendencia):** Es el nuevo extremo alcanzado desde el punto A.
- **La Regla de Oro:** El nivel A es el punto crítico. Si el precio **cierra por debajo de A** (en tendencia alcista) o **por encima de A** (en tendencia bajista), la lógica de esa tendencia ha muerto.

## 2. Definición Algorítmica (PolyTrend)
- **Regla del Cuerpo (Body Rule):** Polytrend se centra en los niveles de **Apertura y Cierre (Open/Close)** de las velas de inflexión, ignorando las mechas para el trazado de niveles.
- **Identificación de A:** Es el *swing low* que origina el movimiento hacia un nuevo máximo (B).
- **Validación por Cierre:** Solo el **cierre de vela** invalida la tendencia. Las mechas que traspasan el nivel A sin cerrar por debajo se consideran "ruido" o testeos, no invalidaciones.

## 3. Flujo de Lógica (Logic Flow)
- El mercado no se mueve por "manipulaciones", sino por una **progresión lógica**.
- Si el soporte A se mantiene, la probabilidad sigue estando a favor de la continuación hacia B o nuevos máximos.
- El sistema utiliza un **ZigZag Adaptativo** para identificar estos puntos A y B de forma automática.

## 4. Implementación en v27
- **Niveles:** Se dibujan en el `Math.Min(Open, Close)` para soportes y `Math.Max(Open, Close)` para resistencias.
- **Multi-Timeframe:** Los niveles de TFs superiores (H1, H4, D1) sirven como imanes o zonas de mayor relevancia para el precio en TFs menores.
