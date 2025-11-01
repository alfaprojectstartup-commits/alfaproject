# Alfa API

Esta API expõe operações para gerenciar processos, fases e respostas. Todos os endpoints requerem o cabeçalho `X-Empresa-Id` com o identificador da empresa em nome da qual a requisição está sendo efetuada.

## Endpoints principais

### Processos (`/api/processos`)

| Método | Rota | Descrição |
| ------ | ---- | --------- |
| `POST` | `/api/processos` | Cria um novo processo a partir de fases modelo informadas. Retorna o identificador do processo criado. |
| `GET` | `/api/processos/{id}` | Recupera os detalhes do processo, incluindo fases, páginas e campos já respondidos. |
| `GET` | `/api/processos/{id}/fases` | Lista as fases instanciadas do processo com o percentual de progresso calculado para cada uma. |
| `POST` | `/api/processos/{id}/respostas` | Registra as respostas de uma página do processo, recalculando automaticamente os percentuais da fase e do processo. |

### Respostas (`/api/respostas`)

| Método | Rota | Descrição |
| ------ | ---- | --------- |
| `POST` | `/api/respostas/pagina` | Endpoint legado que continua aceitando o envio das respostas de uma página. Internamente ele reaproveita a mesma lógica do endpoint aninhado em processos e também atualiza os percentuais de progresso. |

### Fases (`/api/fases`)

| Método | Rota | Descrição |
| ------ | ---- | --------- |
| `GET` | `/api/fases/modelos` | Lista os modelos de fases disponíveis para a empresa. |

## Cálculo de progresso

Quando uma página é respondida, os campos obrigatórios preenchidos determinam o percentual de conclusão da página. O progresso da fase é a média dos percentuais de todas as suas páginas e o progresso do processo é calculado como a média dos progressos de suas fases. Ambos os percentuais são arredondados para o inteiro mais próximo.
