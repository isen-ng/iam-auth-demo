## Requirements

* Python 3
  * `brew install pyenv`
  * `pyenv install 3.7.7`
  * `pyenv global 3.7.7`
  * `echo -e 'if command -v pyenv 1>/dev/null 2>&1; then\n  eval "$(pyenv init -)"`
  * `source ~/.bash_profile`
* Your own virtual environment
  * `python -m venv venv`
  * `. ./venv/bin/activate`
  * `pip install -r requirements.txt`
  
## Caveats

It is important to note at the point of creating this demo (April, 2020), Postgres for RDS and RDS Aurora with postgres
compatibility *does not support logical replication connections*. 