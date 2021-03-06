#include "VirtualMachine.h"
#include <cstring>
#include <iostream>

inline void VirtualMachine::Resize()
{
#ifdef _DEBUG
	Log("Resize " + std::to_string(m_nCapacity));
#endif

	HeapCollect(); //is here the best place for it?

	const int inc = m_nCapacity >> 1;
	Variant* pStack = new Variant[m_nCapacity + inc];
	memcpy(pStack, m_pStack, (byte*)(m_bp + 1) - (byte*)m_pStack);
	Variant* sp = pStack + (m_sp - m_pStack + inc);
	memcpy(sp, m_sp, (byte*)(m_pStack + m_nCapacity) - (byte*)m_sp);
	m_sp = sp;
	m_bp = m_bp >= m_pStack ? pStack + (m_bp - m_pStack) : pStack - 1;
	delete[](m_pStack);
	m_pStack = pStack;
	m_nCapacity = m_nCapacity + inc;
}

void VirtualMachine::CheckReferences(Variant* from, Variant* to)
{
	Variant* pValue = (Variant*)from->pValue;
	if (pValue)
	{
		const unsigned short type = from->usType;

		if (type == VarType::STR)
		{
			to->pValue = pValue;
			return;
		}

		if (pValue->nReplaced == 0)
		{
			const unsigned int length = from->nLength;
			Variant* pGlobDesc1 = pValue;
			const unsigned int capacity = pGlobDesc1->nCap;
			Variant* pGlobalDesc2 = HeapAlloc(capacity);
			to->pValue = pGlobalDesc2;

			if (type == ARR)
			{
				auto f = [this](Variant* elem1, Variant* elem2)
				{
					HeapMove(elem1, elem2);
				};

				Variant::ForEach2(from, to, f);
			}
			else if (type == VarType::DICT)
			{
				Variant* pBucketDesc = HeapAllocStructArr(length);
				unsigned long bucketCap = pBucketDesc->nCap * BUCKET_SIZE;
				Variant* pBucketArr = (Variant*)(pBucketDesc + 1);
				Variant* pBucketStop = pBucketArr + bucketCap;
				Variant* pBucket;

				auto f = [&](Variant* elem1, Variant* elem2)
				{
					if (elem1->pValue)
					{
						elem2->lValue = elem1->lValue;
						void** prev = &elem2->pValue;
						pBucket = (Variant*)elem1->pValue;

						while (pBucket)
						{
							HeapMove(pBucket + KEY, pBucketArr + VALUE);
							HeapMove(pBucket + KEY, pBucketArr + VALUE);
							*prev = pBucketArr;
							pBucket = (Variant*)(pBucket + NEXT)->pValue;
							prev = &(pBucketArr + NEXT)->pValue;
							pBucketArr += BUCKET_SIZE;

							if (pBucketArr == pBucketStop)
							{
								pBucketDesc = (Variant*)pBucketDesc->pValue;
								bucketCap = pBucketDesc->nCap * BUCKET_SIZE;
								pBucketArr = pBucketDesc + 1;
								pBucketStop = pBucketArr + bucketCap;
							}
						}
					}
				};

				Variant::ForEach2(from, to, f);
			}

			pGlobDesc1->nReplaced = 1;
			pGlobDesc1->pValue = to->pValue;
		}
		else
		{
			to->pValue = pValue->pValue;
		}
	}
}

void VirtualMachine::HeapCollect()
{
	HeapChunk* pFirstChunk = m_pCurrentChunk = new HeapChunk;

#ifdef _DEBUG
	++g_memChunk;
#endif

	m_pCurrentSlot = m_pCurrentChunk->vData;

	for (Variant* bp = m_pStack; bp <= m_bp; ++bp)
	{
		CheckReferences(bp, bp);
	}

	const Variant* pStop = m_pStack + m_nCapacity;
	for (Variant* sp = m_sp; sp < pStop; ++sp)
	{
		CheckReferences(sp, sp);
	}

	HeapChunk* iter = m_pFirstChunk;
	m_pFirstChunk = pFirstChunk;
	while (iter)
	{
		HeapChunk* next = iter->pNext;

#ifdef _DEBUG
		if (!next)
		{
			const unsigned short diff = (unsigned short)(m_pCurrentSlot - iter->vData);
			g_memVar -= diff;
		}
		else
		{
			g_memVar -= c_nChunkCapacity;
		}
		--g_memChunk;
#endif

		delete(iter);
		iter = next;
	}

#ifdef _DEBUG
	LogMemory();
#endif
}

void VirtualMachine::HeapMove(Variant* from, Variant* to)
{
	from->Copy();
	to->dValue = from->dValue;

	CheckReferences(from, to);
}

VirtualMachine::HeapChunk::~HeapChunk()
{
	for (Variant* iter = vData; iter < vData + c_nChunkSize; ++iter)
	{
		iter->Free();
	}
}

Variant VirtualMachine::FromBytes()
{
	Variant var(*((double*)m_pc));
	m_pc += sizeof(double);

	if (var.usNull == Variant::c_null)
	{
		const unsigned int length = var.nLength;
		if (var.usType == VarType::STR)
		{
			char* str = new char[length + sizeof(unsigned int)];
			*((unsigned int*)str) = 1;
			str += sizeof(unsigned int);
			memcpy(str, m_pc, length);
			m_pc += length;
			var.pValue = str;
		}
		else if (var.usType == VarType::ARR)
		{
			Variant* pArr = VirtualMachine::HeapAlloc(var.nLength);
			var.pValue = pArr;
			auto f = [this](Variant* elem) { *elem = FromBytes(); };
			var.ForEach(f);
		}
		else if (var.usType == VarType::DICT)
		{
			const unsigned int length = var.nLength;
			const unsigned int cap = DictSizGen(length);
			Variant* pGlobalDesc = VirtualMachine::HeapAlloc(cap);
			var.pValue = pGlobalDesc;
			Variant* pBucketDesc = VirtualMachine::HeapAllocStructArr(length);

			while (pBucketDesc)
			{
				const unsigned int bucketCap = pBucketDesc->nCap * BUCKET_SIZE;
				Variant* pBucketArr = (Variant*)(pBucketDesc + 1);
				for (Variant* pBucket = pBucketArr; pBucket < pBucketArr + bucketCap; pBucket += BUCKET_SIZE)
				{
					Variant key = FromBytes();
					const lldiv_t div = std::div((long long)key.GetHash(), (long long)cap);
					const unsigned int index = (unsigned int)div.rem;

					Variant* cell = var.Get(index);
					*(pBucket + KEY) = key;
					*(pBucket + VALUE) = FromBytes();
					(pBucket + NEXT)->lValue = (unsigned long)div.quot;

					if (cell->pValue == nullptr)
					{
						cell->lValue = 1;
						cell->pValue = pBucket;
					}
					else
					{
						(pBucket + NEXT)->pValue = cell->pValue;
						++cell->lValue;
						cell->pValue = pBucket;
					}
				}

				pBucketDesc = (Variant*)pBucketDesc->pValue;
			}
		}
	}

	return var;
}

void VirtualMachine::Run(byte* program)
{
	m_pc = program;

	try
	{
		while (true)
		{
			switch ((ByteCommand) * (m_pc++))
			{
			case ByteCommand::CALL:
			{
				const int mark = *((long*)m_pc);
				m_pc += sizeof(long long);

#ifdef _DEBUG
				Log("CALL " + std::to_string(mark));
#endif

				if (m_bp + 1 == m_sp)
				{
					Resize();
				}
				(++m_bp)->lValue = (long)(m_pc - program);
				m_pc = program + mark;
			}
			break;
			case ByteCommand::RET:
			{
#ifdef _DEBUG
				Log("RET");
#endif

				m_pc = program + (long)(m_bp--)->lValue;
			}
			break;
			case ByteCommand::FETCH:
			{
				if (m_sp - 1 == m_bp)
				{
					Resize();
				}
				const int offset = *((int*)m_pc);
				m_pc += sizeof(int);

#ifdef _DEBUG
				Log("FETCH " + std::to_string(offset));
#endif

				* (--m_sp) = *(m_pStack + m_nCapacity - offset);
				m_sp->Copy();
			}
			break;
			case ByteCommand::STORE:
			{
				const int offset = *((int*)m_pc);
				m_pc += sizeof(int);

#ifdef _DEBUG
				Log("STORE " + m_sp->ToString() + " " + std::to_string(offset));
#endif

				* (m_pStack + m_nCapacity - offset) = *(m_sp++);
			}
			break;
			case ByteCommand::LFETCH:
			{
				if (m_sp - 1 == m_bp)
				{
					Resize();
				}
				const int offset = *((int*)m_pc);
				m_pc += sizeof(int);

#ifdef _DEBUG
				Log("LFETCH " + std::to_string(offset));
#endif

				* (--m_sp) = *(m_bp - offset);
				m_sp->Copy();
			}
			break;
			case ByteCommand::LSTORE:
			{
				const int offset = *((int*)m_pc);
				m_pc += sizeof(int);

#ifdef _DEBUG
				Log("LSTORE " + m_sp->ToString() + " " + std::to_string(offset));
#endif

				* (m_bp - offset) = *(m_sp++);
			}
			break;
			case ByteCommand::AFETCH:
			{
				const int offset = *((int*)m_pc);
				m_pc += sizeof(int);

				if (m_sp->usNull == Variant::c_null)
				{
					throw Variant::ex_wrongType((VarType)m_sp->usType);
				}

				const unsigned int index = (unsigned int)m_sp->dValue;

#ifdef _DEBUG
				Log("AFETCH " + std::to_string(offset) + " " + std::to_string(index));

				const clock_t tStart = clock();
#endif

				Variant* arr = m_pStack + m_nCapacity - offset;
				arr->CheckBounds(index);
				Variant* var = arr->Get(index);
				var->Copy();
				*m_sp = *var;

#ifdef _DEBUG
				const clock_t tEnd = clock();
				LogTime(tEnd - tStart);
#endif
			}
			break;
			case ByteCommand::ASTORE:
			{
				const int offset = *((int*)m_pc);
				m_pc += sizeof(int);

				if (m_sp->usNull == Variant::c_null)
				{
					throw Variant::ex_wrongType((VarType)m_sp->usType);
				}

				const unsigned int index = (unsigned int)(m_sp++)->dValue;

#ifdef _DEBUG
				Log("ASTORE " + m_sp->ToString() + " " + std::to_string(offset) + " " + std::to_string(index));

				const clock_t tStart = clock();
#endif

				Variant* arr = m_pStack + m_nCapacity - offset;
				arr->CheckBounds(index);
				*(arr->Get(index)) = *(m_sp++);

#ifdef _DEBUG
				const clock_t tEnd = clock();
				LogTime(tEnd - tStart);
#endif
			}
			break;
			case ByteCommand::APUSH:
			{
				const int offset = *((int*)m_pc);
				m_pc += sizeof(int);

#ifdef _DEBUG
				Log("APUSH " + m_sp->ToString() + " " + std::to_string(offset));

				const clock_t tStart = clock();
#endif

				Variant* arr = m_pStack + m_nCapacity - offset;
				arr->CheckType(VarType::ARR);

				Variant* pGlobalDesc = (Variant*)arr->pValue;
				unsigned int cap = pGlobalDesc->nCap;
				const unsigned int len = arr->nLength;
				if (len == cap)
				{
					unsigned int inc = cap >> Variant::c_capInc;

					if (inc == 0)
					{
						inc = 1;
					}

					Variant* pLocalDesc = VirtualMachine::HeapAlloc(inc, true);
					*(pLocalDesc + 1) = *(m_sp++);
					arr->PushBack(pLocalDesc);
					pGlobalDesc->nCap += inc;
				}
				else
				{
					*(arr->Get(len)) = *(m_sp++);
				}

				++arr->nLength;

#ifdef _DEBUG
				const clock_t tEnd = clock();
				LogTime(tEnd - tStart);
#endif
			}
			break;
			case ByteCommand::DFETCH:
			{
				const int offset = *((int*)m_pc);
				m_pc += sizeof(int);

#ifdef _DEBUG
				Log("DFETCH " + std::to_string(offset) + " " + m_sp->ToString());

				const clock_t tStart = clock();
#endif

				Variant* val = (m_pStack + m_nCapacity - offset)->Find(m_sp);
				val->Copy();
				m_sp->Free();
				*m_sp = *val;

#ifdef _DEBUG
				const clock_t tEnd = clock();
				LogTime(tEnd - tStart);
#endif
			}
			break;
			case ByteCommand::DSTORE:
			{
				const int offset = *((int*)m_pc);
				m_pc += sizeof(int);

				Variant* key = m_sp++;

#ifdef _DEBUG
				Log("DSTORE " + m_sp->ToString() + " " + std::to_string(offset) + " " + key->ToString());

				const clock_t tStart = clock();
#endif

				Variant* dict = m_pStack + m_nCapacity - offset;
				dict->CheckType(VarType::DICT);
				*(dict->Find(key)) = *(m_sp++);
				key->Free();

#ifdef _DEBUG
				const clock_t tEnd = clock();
				LogTime(tEnd - tStart);
#endif
			}
			break;
			case ByteCommand::DINSERT:
			{
				const int offset = *((int*)m_pc);
				m_pc += sizeof(int);

				Variant* key = m_sp++;

#ifdef _DEBUG
				Log("DINS " + m_sp->ToString() + " " + std::to_string(offset) + " " + key->ToString());

				const clock_t tStart = clock();
#endif
				Variant* var = m_pStack + m_nCapacity - offset;
				var->CheckType(VarType::DICT);
				Variant* bucket = VirtualMachine::HeapAllocStruct();

				const unsigned int capacity = ((Variant*)var->pValue)->nCap;
				const lldiv_t div = std::div((long long)key->GetHash(), (long long)capacity);
				const unsigned int index = (unsigned int)div.rem;
				Variant* entry = (Variant*)var->Get(index);

				*(bucket + KEY) = *key;
				*(bucket + VALUE) = *(m_sp++);
				(bucket + NEXT)->lValue = (unsigned long)div.quot;
				var->Insert(bucket, entry);
				++var->nLength;

				if (entry->lValue > Variant::c_maxListSize)
				{
					Variant* pLocalDesc = HeapAlloc(capacity, true);
					var->DictResize(pLocalDesc);
				}

#ifdef _DEBUG
				const clock_t tEnd = clock();
				LogTime(tEnd - tStart);
#endif
			}
			break;
			case ByteCommand::LALLOC:
			{
				const int size = *((int*)m_pc);
				m_pc += sizeof(int);

#ifdef _DEBUG
				Log("LALLOC " + std::to_string(size));
#endif

				while (m_bp + size + 1 >= m_sp)
				{
					Resize();
				}
				m_bp = m_bp + size + 1;
				m_bp->dValue = size;
			}
			break;
			case ByteCommand::LFREE:
			{
#ifdef _DEBUG
				Log("LFREE");
#endif

				Variant* bp = m_bp - 1 - (int)(m_bp--)->dValue;
				while (m_bp > bp)
				{
					(m_bp--)->Free();
				}
			}
			break;
			case ByteCommand::ARRAY:
			{
				if (m_sp->usNull == Variant::c_null)
				{
					throw Variant::ex_wrongType((VarType)m_sp->usType);
				}

				const unsigned int len = (unsigned short)m_sp->dValue;

#ifdef _DEBUG
				Log("ARR " + std::to_string(len));

				const clock_t tStart = clock();
#endif

				unsigned int capacity = len + (len >> Variant::c_capInc);
				static const unsigned char minCapacity = 8;
				if (capacity < minCapacity)
				{
					capacity = minCapacity;
				}
				Variant* arr = VirtualMachine::HeapAlloc(capacity);
				*m_sp = Variant(arr, len, VarType::ARR);

#ifdef _DEBUG
				const clock_t tEnd = clock();
				LogTime(tEnd - tStart);
#endif
			}
			break;
			case ByteCommand::DICTIONARY:
			{
				if (m_sp->usNull == Variant::c_null)
				{
					throw Variant::ex_wrongType((VarType)m_sp->usType);
				}

				const unsigned int len = (unsigned int)m_sp->dValue;

#ifdef _DEBUG
				Log("DICT " + std::to_string(len));

				const clock_t tStart = clock();
#endif

				const unsigned int capacity = DictSizGen(len);
				Variant* dict = VirtualMachine::HeapAlloc(capacity);
				*m_sp = Variant(dict, 0, VarType::DICT);

#ifdef _DEBUG
				const clock_t tEnd = clock();
				LogTime(tEnd - tStart);
#endif
			}
			break;
			case ByteCommand::CONCAT:
			{
				Variant* op2 = m_sp;
				Variant* op1 = ++m_sp;

#ifdef _DEBUG
				Log("CONCAT " + op1->ToString() + " " + op2->ToString());

				const clock_t tStart = clock();
#endif

				op1->Concat(op2);
				op2->Free();

#ifdef _DEBUG
				const clock_t tEnd = clock();
				LogTime(tEnd - tStart);
#endif
			}
			break;
			case ByteCommand::APOP:
			{
				const int offset = *((int*)m_pc);
				m_pc += sizeof(int);

#ifdef _DEBUG
				Log("APOP " + std::to_string(offset));

				const clock_t tStart = clock();
#endif

				Variant* arr = m_pStack + m_nCapacity - offset;
				arr->CheckType(VarType::ARR);
				arr->PopBack();

#ifdef _DEBUG
				const clock_t tEnd = clock();
				LogTime(tEnd - tStart);
#endif
			}
			break;
			case ByteCommand::DERASE:
			{
				const int offset = *((int*)m_pc);
				m_pc += sizeof(int);

#ifdef _DEBUG
				Log("DERASE " + std::to_string(offset) + " " + m_sp->ToString());

				const clock_t tStart = clock();
#endif

				Variant* dict = m_pStack + m_nCapacity - offset;
				dict->CheckType(VarType::DICT);
				dict->Erase(m_sp);
				(m_sp++)->Free();

#ifdef _DEBUG
				const clock_t tEnd = clock();
				LogTime(tEnd - tStart);
#endif
			}
			break;
			case ByteCommand::PUSH:
			{
				if (m_sp - 1 == m_bp)
				{
					Resize();
				}
				*(--m_sp) = FromBytes();

#ifdef _DEBUG
				Log("PUSH " + m_sp->ToString());
#endif
			}
			break;
			case ByteCommand::POP:
			{
#ifdef _DEBUG
				Log("POP " + m_sp->ToString());
#endif

				(m_sp++)->Free();
			}
			break;
			case ByteCommand::ADD:
			{
				Variant* op2 = m_sp++;
				Variant* op1 = m_sp;

#ifdef _DEBUG
				Log("ADD " + op1->ToString() + " " + op2->ToString());
#endif

				if (op1->usNull == Variant::c_null || op2->usNull == Variant::c_null)
				{
					throw Variant::ex_wrongType(op1->usNull == Variant::c_null ? (VarType)op1->usType : (VarType)op2->usType);
				}
				op1->dValue += op2->dValue;
			}
			break;
			case ByteCommand::SUB:
			{
				Variant* op2 = m_sp++;
				Variant* op1 = m_sp;

#ifdef _DEBUG
				Log("SUB " + op1->ToString() + " " + op2->ToString());
#endif

				if (op1->usNull == Variant::c_null || op2->usNull == Variant::c_null)
				{
					throw Variant::ex_wrongType(op1->usNull == Variant::c_null ? (VarType)op1->usType : (VarType)op2->usType);
				}
				op1->dValue -= op2->dValue;
			}
			break;
			case ByteCommand::INC:
			{
				const int offset = *((int*)m_pc);
				m_pc += sizeof(int);

#ifdef _DEBUG
				Log("INC " + std::to_string(offset));
#endif

				Variant* var = (m_pStack + m_nCapacity - offset);
				if (var->usNull == Variant::c_null)
				{
					throw Variant::ex_wrongType((VarType)var->usType);
				}
				++var->dValue;
			}
			break;
			case ByteCommand::DEC:
			{
				const int offset = *((int*)m_pc);
				m_pc += sizeof(int);

#ifdef _DEBUG
				Log("DEC " + std::to_string(offset));
#endif

				Variant* var = (m_pStack + m_nCapacity - offset);
				if (var->usNull == Variant::c_null)
				{
					throw Variant::ex_wrongType((VarType)var->usType);
				}
				--var->dValue;
			}
			break;
			case ByteCommand::NEG:
			{
				const int offset = *((int*)m_pc);
				m_pc += sizeof(int);

#ifdef _DEBUG
				Log("NEG " + std::to_string(offset));
#endif

				Variant* var = (m_pStack + m_nCapacity - offset);
				if (var->usNull == Variant::c_null)
				{
					throw Variant::ex_wrongType((VarType)var->usType);
				}
				var->dValue = -var->dValue;
			}
			break;
			case ByteCommand::MULT:
			{
				Variant* op2 = m_sp++;
				Variant* op1 = m_sp;

#ifdef _DEBUG
				Log("MULT " + op1->ToString() + " " + op2->ToString());
#endif

				if (op1->usNull == Variant::c_null || op2->usNull == Variant::c_null)
				{
					throw Variant::ex_wrongType(op1->usNull == Variant::c_null ? (VarType)op1->usType : (VarType)op2->usType);
				}
				op1->dValue *= op2->dValue;
			}
			break;
			case ByteCommand::DIV:
			{
				Variant* op2 = m_sp++;
				Variant* op1 = m_sp;

#ifdef _DEBUG
				Log("DIV " + op1->ToString() + " " + op2->ToString());
#endif

				if (op1->usNull == Variant::c_null || op2->usNull == Variant::c_null)
				{
					throw Variant::ex_wrongType(op1->usNull == Variant::c_null ? (VarType)op1->usType : (VarType)op2->usType);
				}

				if (op2->dValue != 0.0)
				{
					op1->dValue /= op2->dValue;
				}
				else
				{
					throw ex_zeroDiv();
				}
			}
			break;
			case ByteCommand::MOD:
			{
				Variant* op2 = m_sp++;
				Variant* op1 = m_sp;

#ifdef _DEBUG
				Log("MOD " + op1->ToString() + " " + op2->ToString());
#endif

				if (op1->usNull == Variant::c_null || op2->usNull == Variant::c_null)
				{
					throw Variant::ex_wrongType(op1->usNull == Variant::c_null ? (VarType)op1->usType : (VarType)op2->usType);
				}

				if (op2->dValue != 0.0)
				{
					op1->dValue = (long)op1->dValue % (long)op2->dValue;
				}
				else
				{
					throw ex_zeroDiv();
				}
			}
			break;
			case ByteCommand::AND:
			{
				Variant* op2 = m_sp++;
				Variant* op1 = m_sp;

#ifdef _DEBUG
				Log("AND " + op1->ToString() + " " + op2->ToString());
#endif

				if (op1->usNull == Variant::c_null || op2->usNull == Variant::c_null)
				{
					throw Variant::ex_wrongType(op1->usNull == Variant::c_null ? (VarType)op1->usType : (VarType)op2->usType);
				}
				op1->dValue = (long)op1->dValue && (long)op2->dValue;
			}
			break;
			case ByteCommand::OR:
			{
				Variant* op2 = m_sp++;
				Variant* op1 = m_sp;

#ifdef _DEBUG
				Log("OR " + op1->ToString() + " " + op2->ToString());
#endif

				if (op1->usNull == Variant::c_null || op2->usNull == Variant::c_null)
				{
					throw Variant::ex_wrongType(op1->usNull == Variant::c_null ? (VarType)op1->usType : (VarType)op2->usType);
				}
				op1->dValue = (long)op1->dValue || (long)op2->dValue;
			}
			break;
			case ByteCommand::XOR:
			{
				Variant* op2 = m_sp++;
				Variant* op1 = m_sp;

#ifdef _DEBUG
				Log("XOR " + op1->ToString() + " " + op2->ToString());
#endif

				if (op1->usNull == Variant::c_null || op2->usNull == Variant::c_null)
				{
					throw Variant::ex_wrongType(op1->usNull == Variant::c_null ? (VarType)op1->usType : (VarType)op2->usType);
				}
				op1->dValue = (long)op1->dValue ^ (long)op2->dValue;
			}
			break;
			case ByteCommand::BOR:
			{
				Variant* op2 = m_sp++;
				Variant* op1 = m_sp;

#ifdef _DEBUG
				Log("BOR " + op1->ToString() + " " + op2->ToString());
#endif

				if (op1->usNull == Variant::c_null || op2->usNull == Variant::c_null)
				{
					throw Variant::ex_wrongType(op1->usNull == Variant::c_null ? (VarType)op1->usType : (VarType)op2->usType);
				}
				op1->dValue = (long)op1->dValue | (long)op2->dValue;
			}
			break;
			case ByteCommand::BAND:
			{
				Variant* op2 = m_sp++;
				Variant* op1 = m_sp;

#ifdef _DEBUG
				Log("BAND " + op1->ToString() + " " + op2->ToString());
#endif

				if (op1->usNull == Variant::c_null || op2->usNull == Variant::c_null)
				{
					throw Variant::ex_wrongType(op1->usNull == Variant::c_null ? (VarType)op1->usType : (VarType)op2->usType);
				}
				op1->dValue = (long)op1->dValue & (long)op2->dValue;
			}
			break;
			case ByteCommand::INV:
			{
#ifdef _DEBUG
				Log("INV " + m_sp->ToString());
#endif
				if (m_sp->usNull == Variant::c_null)
				{
					throw Variant::ex_wrongType((VarType)m_sp->usType);
				}
				m_sp->dValue = ~(long)m_sp->dValue;
			}
			break;
			case ByteCommand::SHL:
			{
				Variant* op2 = m_sp++;
				Variant* op1 = m_sp;

#ifdef _DEBUG
				Log("SHL " + op1->ToString() + " " + op2->ToString());
#endif

				if (op1->usNull == Variant::c_null || op2->usNull == Variant::c_null)
				{
					throw Variant::ex_wrongType(op1->usNull == Variant::c_null ? (VarType)op1->usType : (VarType)op2->usType);
				}

				op1->dValue = (long)op1->dValue << (long)op2->dValue;
			}
			break;
			case ByteCommand::SHR:
			{
				Variant* op2 = m_sp++;
				Variant* op1 = m_sp;

#ifdef _DEBUG
				Log("SHR " + op1->ToString() + " " + op2->ToString());
#endif

				if (op1->usNull == Variant::c_null || op2->usNull == Variant::c_null)
				{
					throw Variant::ex_wrongType(op1->usNull == Variant::c_null ? (VarType)op1->usType : (VarType)op2->usType);
				}

				op1->dValue = (long)op1->dValue >> (long)op2->dValue;
			}
			break;
			case ByteCommand::NOT:
			{
#ifdef _DEBUG
				Log("NOT " + m_sp->ToString());
#endif
				if (m_sp->usNull == Variant::c_null)
				{
					throw Variant::ex_wrongType((VarType)m_sp->usType);
				}
				m_sp->dValue = m_sp->dValue == 0.0 ? 1.0 : 0.0;
			}
			break;
			case ByteCommand::LT:
			{
				Variant* op2 = m_sp++;
				Variant* op1 = m_sp;

#ifdef _DEBUG
				Log("LT " + op1->ToString() + " " + op2->ToString());
#endif

				if (op1->usNull == Variant::c_null || op2->usNull == Variant::c_null)
				{
					throw Variant::ex_wrongType(op1->usNull == Variant::c_null ? (VarType)op1->usType : (VarType)op2->usType);
				}
				op1->dValue = op1->dValue < op2->dValue ? 1.0 : 0.0;
			}
			break;
			case ByteCommand::GT:
			{
				Variant* op2 = m_sp++;
				Variant* op1 = m_sp;

#ifdef _DEBUG
				Log("GT " + op1->ToString() + " " + op2->ToString());
#endif

				if (op1->usNull == Variant::c_null || op2->usNull == Variant::c_null)
				{
					throw Variant::ex_wrongType(op1->usNull == Variant::c_null ? (VarType)op1->usType : (VarType)op2->usType);
				}
				op1->dValue = op1->dValue > op2->dValue ? 1.0 : 0.0;
			}
			break;
			case ByteCommand::LET:
			{
				Variant* op2 = m_sp++;
				Variant* op1 = m_sp;

#ifdef _DEBUG
				Log("LET " + op1->ToString() + " " + op2->ToString());
#endif

				if (op1->usNull == Variant::c_null || op2->usNull == Variant::c_null)
				{
					throw Variant::ex_wrongType(op1->usNull == Variant::c_null ? (VarType)op1->usType : (VarType)op2->usType);
				}
				op1->dValue = op1->dValue <= op2->dValue ? 1.0 : 0.0;
			}
			break;
			case ByteCommand::GET:
			{
				Variant* op2 = m_sp++;
				Variant* op1 = m_sp;

#ifdef _DEBUG
				Log("GET " + op1->ToString() + " " + op2->ToString());
#endif

				if (op1->usNull == Variant::c_null || op2->usNull == Variant::c_null)
				{
					throw Variant::ex_wrongType(op1->usNull == Variant::c_null ? (VarType)op1->usType : (VarType)op2->usType);
				}
				op1->dValue = op1->dValue >= op2->dValue ? 1.0 : 0.0;
			}
			break;
			case ByteCommand::EQ:
			{
				Variant* op2 = m_sp++;
				Variant* op1 = m_sp;

#ifdef _DEBUG
				Log("EQ " + op1->ToString() + " " + op2->ToString());

				const clock_t tStart = clock();
#endif

				const double bRes = Variant::Equal(op1, op2) ? 1.0 : 0.0;
				op1->Free();
				op2->Free();
				op1->dValue = bRes;

#ifdef _DEBUG
				const clock_t tEnd = clock();
				LogTime(tEnd - tStart);
#endif
			}
			break;
			case ByteCommand::NEQ:
			{
				Variant* op2 = m_sp++;
				Variant* op1 = m_sp;

#ifdef _DEBUG
				Log("NEQ " + op1->ToString() + " " + op2->ToString());

				const clock_t tStart = clock();
#endif

				const double bRes = Variant::Equal(op1, op2) ? 0.0 : 1.0;
				op1->Free();
				op2->Free();
				op1->dValue = bRes;

#ifdef _DEBUG
				const clock_t tEnd = clock();
				LogTime(tEnd - tStart);
#endif
			}
			break;
			case ByteCommand::JZ:
			{
				if ((m_sp++)->dValue == 0.0)
				{
					const long offset = *((long*)m_pc);
					m_pc = program + offset;

#ifdef _DEBUG
					Log("JZ " + std::to_string(offset) + " TRUE");
#endif
				}
				else
				{
					m_pc += sizeof(long long);

#ifdef _DEBUG
					Log("JZ FALSE");
#endif
				}
			}
			break;
			case ByteCommand::JNZ:
			{
				if ((m_sp++)->dValue != 0.0)
				{
					const long offset = *((long*)m_pc);
					m_pc = program + offset;

#ifdef _DEBUG
					Log("JNZ " + std::to_string(offset) + " TRUE");
#endif
				}
				else
				{
					m_pc += sizeof(long long);

#ifdef _DEBUG
					Log("JNZ FALSE");
#endif
				}
			}
			break;
			case ByteCommand::JMP:
			{
				const long offset = *((long*)m_pc);
				m_pc = program + offset;

#ifdef _DEBUG
				Log("JMP " + std::to_string(offset));
#endif
			}
			break;
			case ByteCommand::PRINT:
			{
#ifdef _DEBUG
				Log("PRINT");
#endif

				std::cout << m_sp->ToString() << std::endl;
				(m_sp++)->Free();
			}
			break;
			case ByteCommand::NONE:
			{
#ifdef _DEBUG
				Log("NONE");
#endif
			}
			break;
			case ByteCommand::HALT:
			{
#ifdef _DEBUG
				Log("HALT");
#endif

				return;
			}
			break;
			case ByteCommand::DUP:
			{
#ifdef _DEBUG
				Log("DUP " + m_sp->ToString());
#endif

				if (m_sp->usNull == Variant::c_null && m_sp->usType == VarType::ARR)
				{
					Variant* pGlobalDesc2 = VirtualMachine::HeapAlloc(((Variant*)m_sp->pValue)->nCap);
					*m_sp = m_sp->Duplicate(pGlobalDesc2);
				}
			}
			break;
			case ByteCommand::NARG:
			{
				if (m_sp - 1 == m_bp)
				{
					Resize();
				}
				const byte offset = *((byte*)m_pc);
				m_pc += sizeof(byte);

#ifdef _DEBUG
				Log("NARG " + std::to_string(offset));
#endif

				byte* arg = *(m_bArgs + offset);
				if (offset > c_bArgsCount || !arg)
				{
					throw ex_argDoesntExists(offset);
				}

				*(--m_sp) = Variant(*((double*)arg));
			}
			break;
			case ByteCommand::SARG:
			{
				if (m_sp - 1 == m_bp)
				{
					Resize();
				}
				const byte offset = *((byte*)m_pc);
				m_pc += sizeof(byte);

#ifdef _DEBUG
				Log("SARG " + std::to_string(offset));
#endif

				byte* arg = *(m_bArgs + offset);
				if (offset > c_bArgsCount || !arg)
				{
					throw ex_argDoesntExists(offset);
				}

				char* sArg = reinterpret_cast<char*>(arg);
				const unsigned int len = (unsigned int)(strchr(sArg, '\0') - sArg);
				char* str = new char[len + sizeof(int)];
				*((unsigned int*)str) = 1;
				str += sizeof(unsigned int);
				memcpy(str, sArg, len);
				*(--m_sp) = Variant(str, len);
			}
			break;
			case ByteCommand::ASSERT:
			{
				if ((m_sp++)->dValue == 0.0)
				{
					throw std::exception("FAIL\n");
				}
			}
			break;
			case ByteCommand::LEN:
			{
				const int offset = *((int*)m_pc);
				m_pc += sizeof(int);

#ifdef _DEBUG
				Log("LEN " + std::to_string(offset));
				const clock_t tStart = clock();
#endif

				Variant* var = m_pStack + m_nCapacity - offset;

				if (m_sp - 1 == m_bp)
				{
					Resize();
				}
				(--m_sp)->dValue = var->nLength;

#ifdef _DEBUG
				const clock_t tEnd = clock();
				LogTime(tEnd - tStart);
#endif
			}
			break;
			case ByteCommand::DARR:
			{
				const int offset = *((int*)m_pc);
				m_pc += sizeof(int);

#ifdef _DEBUG
				Log("DARR " + std::to_string(offset));
				const clock_t tStart = clock();
#endif

				Variant* dict = m_pStack + m_nCapacity - offset;
				dict->CheckType(VarType::DICT);
				const unsigned int arrLen = dict->nLength << 1;
				Variant* pGlobalDesc = HeapAlloc(arrLen);
				dict->DictToArr(pGlobalDesc);

				if (m_sp - 1 == m_bp)
				{
					Resize();
				}
				*(--m_sp) = Variant(pGlobalDesc, arrLen, VarType::ARR);

#ifdef _DEBUG
				const clock_t tEnd = clock();
				LogTime(tEnd - tStart);
#endif
			}
			break;
			case ByteCommand::DCONT:
			{
				const int offset = *((int*)m_pc);
				m_pc += sizeof(int);

#ifdef _DEBUG
				Log("DCONT " + std::to_string(offset) + " " + m_sp->ToString());
				const clock_t tStart = clock();
#endif

				Variant* dict = m_pStack + m_nCapacity - offset;
				dict->CheckType(VarType::DICT);
				bool res = dict->Contains(m_sp);
				m_sp->Free();
				m_sp->dValue = res;

#ifdef _DEBUG
				const clock_t tEnd = clock();
				LogTime(tEnd - tStart);
#endif
			}
			break;
			case SFETCH:
			{
				const int offset = *((int*)m_pc);
				m_pc += sizeof(int);

				if (m_sp->usNull == Variant::c_null)
				{
					throw Variant::ex_wrongType((VarType)m_sp->usType);
				}

				const unsigned int index = (unsigned int)m_sp->dValue;

#ifdef _DEBUG
				Log("SFETCH " + std::to_string(offset) + " " + std::to_string(index));

				const clock_t tStart = clock();
#endif

				Variant* str = m_pStack + m_nCapacity - offset;

				str->CheckBounds(index);
				str->CheckType(VarType::STR);

				char* chr = new char[1 + sizeof(unsigned int)];
				*(unsigned int*)chr = 1;
				chr += sizeof(unsigned int);
				*chr = *((char*)str->pValue + index);
				*m_sp = Variant(chr, 1);

#ifdef _DEBUG
				const clock_t tEnd = clock();
				LogTime(tEnd - tStart);
#endif
			}
			break;
			case SSTORE:
			{
				const int offset = *((int*)m_pc);
				m_pc += sizeof(int);

				if (m_sp->usNull == Variant::c_null)
				{
					throw Variant::ex_wrongType((VarType)m_sp->usType);
				}

				const unsigned int index = (unsigned int)(m_sp++)->dValue;

				m_sp->CheckType(VarType::STR);

#ifdef _DEBUG
				Log("SSTORE " + m_sp->ToString() + " " + std::to_string(offset) + " " + std::to_string(index));

				const clock_t tStart = clock();
#endif

				Variant* str = m_pStack + m_nCapacity - offset;
				str->CheckBounds(index);
				*((char*)str->pValue + index) = *((char*)m_sp->pValue);
				(m_sp++)->Free();

#ifdef _DEBUG
				const clock_t tEnd = clock();
				LogTime(tEnd - tStart);
#endif
			}
			break;
			case NTOS:
			{
				if (m_sp->usNull == Variant::c_null)
				{
					throw Variant::ex_wrongType((VarType)m_sp->usType);
				}

#ifdef _DEBUG
				Log("NTOS " + m_sp->ToString());
#endif

				const double num = m_sp->dValue;
				const unsigned int len = _scprintf("%f", num);
				char* str = new char[len + sizeof(unsigned int)];
				*(unsigned int*)str = 1;
				str += sizeof(unsigned int);
				sprintf_s(str, len + 1, "%f", num);
				*m_sp = Variant(str, len);
			}
			break;
			case STON:
			{
				m_sp->CheckType(VarType::STR);

#ifdef _DEBUG
				Log("STON " + m_sp->ToString());
#endif

				char* str = (char*)m_sp->pValue;
				char* strEnd = str + m_sp->nLength;
				double num = strtod(str, &strEnd);
				m_sp->Free();
				*m_sp = Variant(num);
			}
			break;
			case SMATCH:
			{
				const int offset = *((int*)m_pc);
				m_pc += sizeof(int);

				m_sp->CheckType(VarType::STR);

#ifdef _DEBUG
				Log("SMATCH " + std::to_string(offset) + " " + m_sp->ToString());

				clock_t tStart = clock();
#endif

				Variant* str = m_pStack + m_nCapacity - offset;
				const long res = str->Match(m_sp);
				m_sp->Free();
				*m_sp = Variant((double)res);

#ifdef _DEBUG
				const clock_t tEnd = clock();
				LogTime(tEnd - tStart);
#endif
			}
			break;
			case SUBS:
			{
				const int offset = *((int*)m_pc);
				m_pc += sizeof(int);

				if (m_sp->usNull == Variant::c_null)
				{
					throw Variant::ex_wrongType((VarType)m_sp->usType);
				}
				const unsigned int start = (unsigned int)(m_sp++)->dValue;

				if (m_sp->usNull == Variant::c_null)
				{
					throw Variant::ex_wrongType((VarType)m_sp->usType);
				}
				const unsigned int end = (unsigned int)(m_sp)->dValue;

#ifdef _DEBUG
				Log("SUBS " + std::to_string(offset) + " " + std::to_string(start) + " " + std::to_string(end));
#endif

				Variant* str = m_pStack + m_nCapacity - offset;

				str->CheckType(VarType::STR);
				str->CheckBounds(start);
				str->CheckBounds(end);

				const unsigned int len = end - start;
				char* substr = new char[sizeof(unsigned int) + len];
				*(unsigned int*)substr = 1;
				substr += sizeof(unsigned int);
				memcpy(substr, (char*)str->pValue + start, len);
				*m_sp = Variant(substr, len);
			}
			break;
			default:
			{
				return;
			}
			}
		}
	}
	catch (exception e)
	{
		throw exception((std::to_string(m_pc - program) + ": " + e.what()).c_str());
	}
}

extern "C"
{
	__declspec(dllexport) void __stdcall Run0(byte* program)
	{
		VirtualMachine vm;
		vm.Run(program);
	}

	__declspec(dllexport) void __stdcall Run1(byte* program, byte* arg0)
	{
		VirtualMachine vm;
		vm.ProvideArgs(arg0);
		vm.Run(program);
	}

	__declspec(dllexport) void __stdcall Run2(byte* program, byte* arg0, byte* arg1)
	{
		VirtualMachine vm;
		vm.ProvideArgs(arg0, arg1);
		vm.Run(program);
	}

	__declspec(dllexport) void __stdcall Run3(byte* program, byte* arg0, byte* arg1, byte* arg2)
	{
		VirtualMachine vm;
		vm.ProvideArgs(arg0, arg1, arg2);
		vm.Run(program);
	}

	__declspec(dllexport) void __stdcall Run4(byte* program, byte* arg0, byte* arg1, byte* arg2, byte* arg3)
	{
		VirtualMachine vm;
		vm.ProvideArgs(arg0, arg1, arg2, arg3);
		vm.Run(program);
	}

	__declspec(dllexport) double __stdcall NumRun0(byte* program)
	{
		VirtualMachine vm;
		vm.Run(program);
		double num = vm.Return().dValue;
		return num;
	}

	__declspec(dllexport) double __stdcall NumRun1(byte* program, byte* arg0)
	{
		VirtualMachine vm;
		vm.ProvideArgs(arg0);
		vm.Run(program);
		double num = vm.Return().dValue;
		return num;
	}

	__declspec(dllexport) double __stdcall NumRun2(byte* program, byte* arg0, byte* arg1)
	{
		VirtualMachine vm;
		vm.ProvideArgs(arg0, arg1);
		vm.Run(program);
		double num = vm.Return().dValue;
		return num;
	}

	__declspec(dllexport) double __stdcall NumRun3(byte* program, byte* arg0, byte* arg1, byte* arg2)
	{
		VirtualMachine vm;
		vm.ProvideArgs(arg0, arg1, arg2);
		vm.Run(program);
		double num = vm.Return().dValue;
		return num;
	}

	__declspec(dllexport) double __stdcall NumRun4(byte* program, byte* arg0, byte* arg1, byte* arg2, byte* arg3)
	{
		VirtualMachine vm;
		vm.ProvideArgs(arg0, arg1, arg2, arg3);
		vm.Run(program);
		double num = vm.Return().dValue;
		return num;
	}

	__declspec(dllexport) int __stdcall StrRun0(byte* program, char* res)
	{
		VirtualMachine vm;
		vm.Run(program);
		Variant var = vm.Return();
		memcpy(res, var.pValue, var.nLength);
		return var.nLength;
	}

	__declspec(dllexport) int __stdcall StrRun1(byte* program, char* res, byte* arg0)
	{
		VirtualMachine vm;
		vm.ProvideArgs(arg0);
		vm.Run(program);
		Variant var = vm.Return();
		memcpy(res, var.pValue, var.nLength);
		return var.nLength;
	}

	__declspec(dllexport) int __stdcall StrRun2(byte* program, char* res, byte* arg0, byte* arg1)
	{
		VirtualMachine vm;
		vm.ProvideArgs(arg0, arg1);
		vm.Run(program);
		Variant var = vm.Return();
		memcpy(res, var.pValue, var.nLength);
		return var.nLength;
	}

	__declspec(dllexport) int __stdcall StrRun3(byte* program, char* res, byte* arg0, byte* arg1, byte* arg2)
	{
		VirtualMachine vm;
		vm.ProvideArgs(arg0, arg1, arg2);
		vm.Run(program);
		Variant var = vm.Return();
		memcpy(res, var.pValue, var.nLength);
		return var.nLength;
	}

	__declspec(dllexport) int __stdcall StrRun4(byte* program, char* res, byte* arg0, byte* arg1, byte* arg2, byte* arg3)
	{
		VirtualMachine vm;
		vm.ProvideArgs(arg0, arg1, arg2, arg3);
		vm.Run(program);
		Variant var = vm.Return();
		memcpy(res, var.pValue, var.nLength);
		return var.nLength;
	}
}