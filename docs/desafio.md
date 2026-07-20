# Desafio Arquiteto de Soluções — Fluxo de Caixa

## Problema

Um comerciante precisa controlar o fluxo de caixa diário (débitos e créditos) e consultar o saldo diário consolidado.

## Requisitos de negócio

- Serviço de controle de lançamentos
- Serviço de consolidado diário

## Requisitos obrigatórios

- Mapeamento de domínios funcionais e capacidades de negócio
- Refinamento de requisitos funcionais e não funcionais
- Desenho da solução completa (Arquitetura Alvo)
- Justificativa de ferramentas, tecnologias e tipo de arquitetura
- Implementação na linguagem escolhida (.NET)
- Testes automatizados
- README com instruções de execução local
- Repositório público no GitHub

## Requisitos não funcionais

1. O serviço de lançamentos **não pode ficar indisponível** se o consolidado cair.
2. Em picos, o consolidado recebe **50 requisições/segundo** com no máximo **5% de perda**.

## Requisitos diferenciais

- Arquitetura de transição (migração de legado)
- Estimativa de custos de infraestrutura
- Monitoramento e observabilidade
- Critérios de segurança na integração entre serviços

## Referência

Enunciado original: `desafio-arquiteto-solucao-ago2024.pdf`
