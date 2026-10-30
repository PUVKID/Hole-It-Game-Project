using System.Collections.Generic;
public static class Korean
{
    private static readonly Dictionary<string,string> words=new Dictionary<string,string>{
        {"POCKET PIT","포켓 홀"},{"SWALLOW  -  GROW  -  CLEAR","가은이의 작은 블랙홀 모험"},
        {"A LITTLE HOLE. A BIG ADVENTURE.","작은 구멍으로 시작하는 커다란 모험"},
        {"PLAY","시작하기"},{"MOVE","움직이기"},{"YOUR MISSION","이 물건을 모아 주세요"},
        {"MISSION COMPLETE!","미션 성공!"},{"LEVEL CLEAR!","잘했어요!"},{"TIME UP","시간이 다 됐어요"},
        {"NEXT LEVEL  >","다음 스테이지  >"},{"HOME","처음으로"},{"TRY AGAIN","다시 도전"},
        {"NEW BEST!","최고 기록!"},{"TOP SCORES","나의 최고 기록"},{"TAKE A BREATHER","잠깐 쉬어가요"},
        {"LET'S PLAY!","계속하기"},{"SETTINGS","설정"},{"LEVELS","스테이지"},{"RESTART","다시 시작"},
        {"REPLAY","한 번 더"},{"CONTROL: DRAG","조작 : 화면 드래그"},{"CONTROL: JOYSTICK","조작 : 조이스틱"},
        {"SOUND: ON","소리 : 켜짐"},{"SOUND: OFF","소리 : 꺼짐"},{"MASTER","전체 음량"},{"MUSIC","배경 음악"},
        {"SOUND EFFECTS","효과음"},{"DONE","확인"},{"CHOOSE A LEVEL","어디로 떠나볼까요?"},{"BACK","돌아가기"},
        {"--:--","여유롭게"},{"LEVEL DATA ERROR","스테이지를 열 수 없어요"},
        {"DONUT","도넛"},{"COOKIE","쿠키"},{"CUPCAKE","컵케이크"},{"CANDY","사탕"},{"RED CAR","빨간 자동차"},
        {"BLUE CAR","파란 자동차"},{"YELLOW CAR","노란 자동차"},{"FRENCH FRIES","감자튀김"},{"BURGER","햄버거"},
        {"RUBBER DUCK","오리 장난감"},{"FLOWER","꽃"},{"TOY BLOCK","블록"},{"SPORT BALL","공"},
        {"TIME BOOST","시간 보너스"},{"ICE POP","아이스바"},{"STREET LAMP","가로등"},{"SODA CAN","음료 캔"},
        {"FIRE HYDRANT","소화전"},{"COFFEE CUP","커피"},{"PARTY CAKE","파티 케이크"},{"TRAFFIC LIGHT","신호등"},
        {"PARK BENCH","벤치"},{"CITY BUS","버스"},{"TOY CAR","장난감 자동차"},{"MILK CARTON","우유"},
        {"BASKETBALL","농구공"},{"BANANA","바나나"},{"DELIVERY CRATE","큰 상자"},{"WOOD CRATE","나무 상자"},
        {"CRATE","상자"},{"TOWER","빌딩"},{"SMALL HOUSE","작은 집"},{"CAKE","케이크"},{"SPORTS CAR","스포츠카"},
        {"CHEESE WEDGE","치즈"},{"TALL CUPCAKE","큰 컵케이크"},{"REFRIGERATOR","냉장고"},{"ICE CREAM","아이스크림"},
        {"FRYING PAN","프라이팬"},{"TOY PIANO","장난감 피아노"},{"BOLLARD","말뚝"},{"PIZZA SLICE","피자"},
        {"POTTED PLANT","화분"},{"SCISSORS","가위"},{"TRAFFIC CONE","안전 고깔"},{"TREE","나무"},
        {"LARGE TREE","큰 나무"},{"OAK TREE","참나무"},{"LARGE WAFFLE","큰 와플"},{"WAFFLE","와플"},{"ROAD BARRIER","울타리"}
    };
    public static string Text(string value){
        if(string.IsNullOrEmpty(value))return value??"";
        string translated;if(words.TryGetValue(value,out translated))return translated;
        return value.Replace("BEST SCORE","최고 기록").Replace("SCORE","점수").Replace("LEVEL ","스테이지 ").Replace("TIER ","크기 ").Replace("SIZE ","크기 ")
            .Replace("Objects  +","수집  +").Replace("Mission  +","미션  +").Replace("Time  +","시간  +").Replace("Clear  +","완료  +")
            .Replace("Dessert Plaza","디저트 광장").Replace("Candy Factory","사탕 공장").Replace("Toy Town","장난감 마을")
            .Replace("Food Truck Court","맛있는 거리").Replace("Pocket Park","초록 공원").Replace("Toy Workshop","장난감 공방");
    }
}
