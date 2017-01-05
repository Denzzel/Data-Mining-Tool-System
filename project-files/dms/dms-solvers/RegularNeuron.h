#pragma once
#include "INeuron.h"
#include "IActivateFunction.h"

namespace dms::solvers::neural_nets
{
	public ref class RegularNeuron : public INeuron
	{
	public:
		RegularNeuron(IActivateFunction^ af) : af(af), w_sum(0.0f) {}
		virtual float getResult(array<float>^ x) override;

		//�������� ���������� ����� �� ���� x, � �������
		//��� ������ ��������� ��� ����� getResult
		virtual float getWeightedSum() override;
	private:
		IActivateFunction^ af;
		float w_sum;
	};
}