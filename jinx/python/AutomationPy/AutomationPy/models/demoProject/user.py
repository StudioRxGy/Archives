from sqlalchemy import Column, Integer, Text
from sqlalchemy.orm import declarative_base

Base = declarative_base()


class User(Base):
    __tablename__ = 'user'

    id = Column(Integer, primary_key=True, index=True)
    name = Column(Text, nullable=False)
    age = Column(Integer)
    sex = Column(Integer)
    phone = Column(Text)
    address = Column(Text)
