/** Type your code here, or load an example. */
#include "https://raw.githubusercontent.com/Neargye/nameof/refs/heads/master/include/nameof.hpp"
#include <tuple>
#include <iostream>

int main()
{
    int aa = 1, bb = 2;
    std::tuple<int const& , int> test(aa, bb);

    auto [a, b] = test;

    int const& a_ref = aa;
    auto a_auto = a_ref;
    decltype(auto) a_auto_ref = a_ref;

    std::cout
        << "   type of structured binding auto | " << NAMEOF_FULL_TYPE_EXPR(a) << std::endl
        << "          type of placeholder auto | " << NAMEOF_FULL_TYPE_EXPR(a_auto) << std::endl
        << "type of placeholder decltype(auto) | " << NAMEOF_FULL_TYPE_EXPR(a_auto_ref) << std::endl
    ;

    return 0;
}