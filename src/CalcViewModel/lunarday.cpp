#include "lunarday.h"
#include <iostream>
#include <string>
#include <vector>

using namespace std;

int getSolarDayOfMonth(int y, int m)
{ // 월별 일수 계산
    if (m != 2)
        return solarDayNum[m - 1];

    if ((y % 400 == 0) || ((y % 100 != 0) && (y % 4 == 0)))
        return 29;
    else
        return 28;
}

int getSolarDayOfYear(int y)
{
    if ((y % 400 == 0) || ((y % 100 != 0) && (y % 4 == 0)))
        return 366;
    else
        return 365;
}


int getTotalDaySolar(int y, int m, int d)
{
    // 양력 2001/01/24 = 음력 2001/01/01
    int solarBasis[] = { 2001, 1, 24 };
    int i;
    int ret = 0;

    for (i = solarBasis[0]; i < y; i++)
        ret += getSolarDayOfYear(i);
    for (i = 1; i < m; i++)
        ret += getSolarDayOfMonth(y, i);
    ret += d;
    for (i = 1; i < solarBasis[1]; i++)
        ret -= getSolarDayOfMonth(solarBasis[0], i);
    ret -= solarBasis[2];

    return ret;
}

vector<int> getLunarDate(int solarYear, int solarMonth, int solarDay)
{
    vector<int> v;
    int y = -1, m = -1, d = 0, f;
    int totalDay = getTotalDaySolar(solarYear, solarMonth, solarDay);

    while (totalDay >= lunarDayOfYear[++y])
        totalDay -= lunarDayOfYear[y];
    while (totalDay >= (d = lunarDayOfMonth[lunarDayOfMonthIndex[y][++m]]))
        totalDay -= d;
    d = totalDay;

    f = lunarDayOfMonthFrac[lunarDayOfMonthIndex[y][m]];
    if (d >= f)
        d -= f;

    v.push_back(y + 1);
    v.push_back(m + 1);
    v.push_back(d + 1);

    return v;
}
