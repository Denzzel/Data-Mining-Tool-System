#pragma once

#include "ICell.h"

namespace dms::solvers::neural_nets
{
	public ref class INeuron abstract
	{
	public:
		INeuron()
		{
			setInitialState();
		}

		~INeuron()
		{
			setInitialState();
		}

		//������������ ��������� �� ������, ��� ����� ���� �������
		void setWeigthsSource(float* src, int wcount);

		//��������� ����� �������. ���� ����� ������� ������� ������
		//�������� ����� �� ��� ����������, ���� �� ����� ����������
	    void setWeights(array<float>^ w);

		array<float>^ getWeights();

		virtual float getResult(array<float>^ x) = 0;
		virtual float getWeightedSum() = 0;
	protected:
		int getWeightsPointer(float* &dest);
	private:
		float* weights_src;
		int weights_src_size;

		void setInitialState();
	};
}
